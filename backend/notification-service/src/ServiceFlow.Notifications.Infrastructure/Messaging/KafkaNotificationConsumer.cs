using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceFlow.Notifications.Application.Abstractions;
using ServiceFlow.Notifications.Application.Contracts;
using ServiceFlow.Notifications.Domain.Entities;
using ServiceFlow.Notifications.Infrastructure.Configuration;
using ServiceFlow.Notifications.Infrastructure.Persistence;

namespace ServiceFlow.Notifications.Infrastructure.Messaging;

public sealed class KafkaNotificationConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<KafkaNotificationConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly KafkaOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Kafka consumer stopped unexpectedly; it will reconnect.");
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.RetryDelaySeconds)), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        using var consumer = BuildConsumer();
        using var deadLetterProducer = BuildDeadLetterProducer();
        consumer.Subscribe(_options.Topic);
        logger.LogInformation(
            "Consuming Kafka topic {Topic} as group {GroupId}.",
            _options.Topic,
            _options.GroupId);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var consumed = consumer.Consume(cancellationToken);
                await HandleMessageAsync(consumer, deadLetterProducer, consumed, cancellationToken);
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private IConsumer<string, string> BuildConsumer() =>
        new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            ClientId = _options.ClientId,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = false
        }).Build();

    private IProducer<string, string> BuildDeadLetterProducer() =>
        new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = $"{_options.ClientId}-dlq",
            Acks = Acks.All,
            EnableIdempotence = true
        }).Build();

    private async Task HandleMessageAsync(
        IConsumer<string, string> consumer,
        IProducer<string, string> deadLetterProducer,
        ConsumeResult<string, string> consumed,
        CancellationToken cancellationToken)
    {
        RequestEventEnvelope integrationEvent;

        try
        {
            integrationEvent = JsonSerializer.Deserialize<RequestEventEnvelope>(
                consumed.Message.Value,
                SerializerOptions)!;
            Validate(integrationEvent);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            logger.LogWarning(exception, "Sending an invalid Kafka integration event to the dead-letter topic.");
            await SendToDeadLetterAsync(deadLetterProducer, consumed, exception, cancellationToken);
            consumer.Commit(consumed);
            return;
        }

        using var logScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["EventId"] = integrationEvent.EventId,
            ["EventType"] = integrationEvent.EventType,
            ["CorrelationId"] = integrationEvent.CorrelationId,
            ["KafkaOffset"] = consumed.TopicPartitionOffset
        });

        Exception? lastException = null;
        for (var attempt = 1; attempt <= _options.MaxProcessingAttempts; attempt++)
        {
            try
            {
                var result = await PersistAndPublishAsync(integrationEvent, cancellationToken);
                consumer.Commit(consumed);
                logger.LogInformation(
                    result.IsDuplicate
                        ? "Duplicate Kafka integration event ignored and offset committed."
                        : "Kafka integration event processed and offset committed.");
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastException = exception;
                logger.LogError(
                    exception,
                    "Kafka event processing failed on attempt {Attempt} of {MaxAttempts}.",
                    attempt,
                    _options.MaxProcessingAttempts);

                if (attempt < _options.MaxProcessingAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.RetryDelaySeconds)), cancellationToken);
                }
            }
        }

        await SendToDeadLetterAsync(deadLetterProducer, consumed, lastException!, cancellationToken);
        consumer.Commit(consumed);
        logger.LogError("Kafka event moved to {DeadLetterTopic} and its source offset was committed.", _options.DeadLetterTopic);
    }

    private async Task<PersistResult> PersistAndPublishAsync(
        RequestEventEnvelope integrationEvent,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var result = await PersistAsync(dbContext, integrationEvent, cancellationToken);

        if (result.Notification is not null)
        {
            var realtimePublisher = scope.ServiceProvider.GetRequiredService<INotificationRealtimePublisher>();
            try
            {
                await realtimePublisher.PublishAsync(integrationEvent, result.Notification, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The notification was saved but its SignalR event failed.");
            }
        }

        return result;
    }

    private async Task SendToDeadLetterAsync(
        IProducer<string, string> producer,
        ConsumeResult<string, string> consumed,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var headers = new Headers
        {
            { "source-topic", Encoding.UTF8.GetBytes(consumed.Topic) },
            { "source-partition", Encoding.UTF8.GetBytes(consumed.Partition.Value.ToString()) },
            { "source-offset", Encoding.UTF8.GetBytes(consumed.Offset.Value.ToString()) },
            { "failure-type", Encoding.UTF8.GetBytes(exception.GetType().Name) },
            { "failure-reason", Encoding.UTF8.GetBytes(Truncate(exception.Message, 1_000)) }
        };

        await producer.ProduceAsync(
            _options.DeadLetterTopic,
            new Message<string, string>
            {
                Key = consumed.Message.Key,
                Value = consumed.Message.Value,
                Headers = headers
            },
            cancellationToken);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static async Task<PersistResult> PersistAsync(
        NotificationDbContext dbContext,
        RequestEventEnvelope integrationEvent,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (await dbContext.ProcessedEvents.AnyAsync(
                processedEvent => processedEvent.EventId == integrationEvent.EventId,
                cancellationToken))
        {
            return PersistResult.Duplicate;
        }

        var notification = NotificationEventFactory.Create(integrationEvent);
        dbContext.ProcessedEvents.Add(ProcessedEvent.Create(integrationEvent.EventId, integrationEvent.EventType));
        dbContext.Notifications.Add(notification);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new PersistResult(false, NotificationDto.FromEntity(notification));
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return PersistResult.Duplicate;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static void Validate(RequestEventEnvelope? integrationEvent)
    {
        if (integrationEvent is null)
        {
            throw new JsonException("The event body is empty.");
        }

        if (integrationEvent.EventId == Guid.Empty)
        {
            throw new ArgumentException("eventId is required.");
        }

        if (string.IsNullOrWhiteSpace(integrationEvent.EventType))
        {
            throw new ArgumentException("eventType is required.");
        }

        if (integrationEvent.EventType.Length > 100)
        {
            throw new ArgumentException("eventType cannot exceed 100 characters.");
        }
    }

    private sealed record PersistResult(bool IsDuplicate, NotificationDto? Notification)
    {
        public static PersistResult Duplicate { get; } = new(true, null);
    }
}
