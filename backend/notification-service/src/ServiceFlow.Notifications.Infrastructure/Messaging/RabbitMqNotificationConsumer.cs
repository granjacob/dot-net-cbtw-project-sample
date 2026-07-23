using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServiceFlow.Notifications.Application.Abstractions;
using ServiceFlow.Notifications.Application.Contracts;
using ServiceFlow.Notifications.Domain.Entities;
using ServiceFlow.Notifications.Infrastructure.Configuration;
using ServiceFlow.Notifications.Infrastructure.Persistence;

namespace ServiceFlow.Notifications.Infrastructure.Messaging;

public sealed class RabbitMqNotificationConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqNotificationConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly RabbitMqOptions _options = options.Value;

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
                logger.LogError(
                    exception,
                    "RabbitMQ consumer stopped unexpectedly. Reconnecting in {DelaySeconds} seconds.",
                    _options.ReconnectDelaySeconds);

                await Task.Delay(
                    TimeSpan.FromSeconds(Math.Max(1, _options.ReconnectDelaySeconds)),
                    stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(Math.Max(1, _options.ReconnectDelaySeconds))
        };

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(
            exchange: _options.DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            queue: _options.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: _options.DeadLetterQueue,
            exchange: _options.DeadLetterExchange,
            routingKey: _options.DeadLetterRoutingKey,
            cancellationToken: cancellationToken);
        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = _options.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = _options.DeadLetterRoutingKey
        };
        await channel.QueueDeclareAsync(
            queue: _options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: _options.Queue,
            exchange: _options.Exchange,
            routingKey: _options.RoutingKey,
            cancellationToken: cancellationToken);
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, eventArgs) =>
            HandleMessageAsync(channel, eventArgs, cancellationToken);

        await channel.BasicConsumeAsync(
            queue: _options.Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Consuming RabbitMQ queue {Queue} from exchange {Exchange} with routing key {RoutingKey}.",
            _options.Queue,
            _options.Exchange,
            _options.RoutingKey);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    private async Task HandleMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        RequestEventEnvelope? integrationEvent;

        try
        {
            integrationEvent = JsonSerializer.Deserialize<RequestEventEnvelope>(
                eventArgs.Body.Span,
                SerializerOptions);

            Validate(integrationEvent);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            logger.LogWarning(exception, "Discarding an invalid notification integration event.");
            await channel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: false, cancellationToken);
            return;
        }

        using var logScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["EventId"] = integrationEvent!.EventId,
            ["EventType"] = integrationEvent.EventType,
            ["CorrelationId"] = integrationEvent.CorrelationId
        });

        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            var result = await PersistAsync(dbContext, integrationEvent, cancellationToken);

            if (result.Notification is not null)
            {
                var realtimePublisher = scope.ServiceProvider
                    .GetRequiredService<INotificationRealtimePublisher>();

                try
                {
                    await realtimePublisher.PublishAsync(
                        integrationEvent,
                        result.Notification,
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    // The durable notification is already committed. SignalR is intentionally best-effort;
                    // connected clients reconcile from the unread/list endpoints after reconnecting.
                    logger.LogError(exception, "The notification was saved but its SignalR event failed.");
                }
            }

            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
            logger.LogInformation(
                result.IsDuplicate
                    ? "Duplicate integration event ignored."
                    : "Integration event processed and notification created.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var requeue = !eventArgs.Redelivered;
            logger.LogError(
                exception,
                requeue
                    ? "Notification event processing failed; message will be retried once."
                    : "Notification event processing failed again; message will be dead-lettered.");
            await channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: requeue,
                cancellationToken);
        }
    }

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
        dbContext.ProcessedEvents.Add(ProcessedEvent.Create(
            integrationEvent.EventId,
            integrationEvent.EventType));
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
