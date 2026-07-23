using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ServiceFlow.Requests.Application.Abstractions;

namespace ServiceFlow.Requests.Infrastructure.Messaging;

public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                logger.LogWarning(exception, "The outbox polling cycle failed; messages will be retried.");
                await ResetConnectionAsync();
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(_options.PollingSeconds, 1, 60)), stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var messages = await repository.GetPendingOutboxAsync(_options.BatchSize, cancellationToken);
        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                await EnsureConnectedAsync(cancellationToken);
                var properties = new BasicProperties
                {
                    ContentType = "application/json",
                    DeliveryMode = DeliveryModes.Persistent,
                    MessageId = message.Id.ToString(),
                    Type = message.EventType,
                    CorrelationId = message.CorrelationId
                };
                await _channel!.BasicPublishAsync(
                    _options.Exchange,
                    _options.RoutingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(message.Payload),
                    cancellationToken: cancellationToken);

                message.MarkProcessed(clock.UtcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Published outbox event {EventId} ({EventType}).", message.Id, message.EventType);
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
                await ResetConnectionAsync();
                break;
            }
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return;
        }

        await ResetConnectionAsync();
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = "serviceflow-request-outbox"
        };
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await _channel.ExchangeDeclareAsync(
            _options.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        await _channel.ExchangeDeclareAsync(
            _options.DeadLetterExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(
            queue: _options.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(
            queue: _options.DeadLetterQueue,
            exchange: _options.DeadLetterExchange,
            routingKey: _options.DeadLetterRoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);
        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = _options.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = _options.DeadLetterRoutingKey
        };
        await _channel.QueueDeclareAsync(
            queue: _options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(
            queue: _options.Queue,
            exchange: _options.Exchange,
            routingKey: _options.BindingKey,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    private async Task ResetConnectionAsync()
    {
        if (_channel is not null)
        {
            try
            {
                await _channel.DisposeAsync();
            }
            catch
            {
                // Connection recovery is best effort; the next polling cycle creates a fresh channel.
            }

            _channel = null;
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch
            {
                // See above.
            }

            _connection = null;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await ResetConnectionAsync();
    }
}
