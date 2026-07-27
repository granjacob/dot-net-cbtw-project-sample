using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceFlow.Requests.Application.Abstractions;
using ServiceFlow.Requests.Domain.Entities;

namespace ServiceFlow.Requests.Infrastructure.Messaging;

public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private readonly KafkaOptions _options = options.Value;
    private IProducer<string, string>? _producer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _producer = BuildProducer();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "The Kafka outbox polling cycle failed; messages will be retried.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(_options.PollingSeconds, 1, 60)), stoppingToken);
        }
    }

    private IProducer<string, string> BuildProducer() => new ProducerBuilder<string, string>(new ProducerConfig
    {
        BootstrapServers = _options.BootstrapServers,
        ClientId = _options.ClientId,
        Acks = Acks.All,
        EnableIdempotence = true,
        MessageSendMaxRetries = int.MaxValue,
        MessageTimeoutMs = checked(_options.MessageTimeoutSeconds * 1_000)
    }).Build();

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var messages = await repository.GetPendingOutboxAsync(_options.BatchSize, cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var result = await _producer!.ProduceAsync(
                    _options.Topic,
                    KafkaOutboxMessageFactory.Create(message),
                    cancellationToken);

                if (result.Status == PersistenceStatus.NotPersisted)
                {
                    throw new KafkaException(new Error(ErrorCode.Local_MsgTimedOut, "Kafka did not persist the event."));
                }

                message.MarkProcessed(clock.UtcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation(
                    "Published outbox event {EventId} ({EventType}) to {TopicPartitionOffset}.",
                    message.Id,
                    message.EventType,
                    result.TopicPartitionOffset);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.MarkFailed(exception.Message);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogWarning(
                    exception,
                    "Failed to publish outbox event {EventId}; attempt {Attempt} will be retried.",
                    message.Id,
                    message.Attempts);
                break;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        _producer?.Flush(cancellationToken);
        _producer?.Dispose();
        _producer = null;
    }
}
