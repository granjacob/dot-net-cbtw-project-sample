namespace ServiceFlow.Notifications.Infrastructure.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; init; } = "rabbitmq";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
    public string Exchange { get; init; } = "serviceflow.events";
    public string Queue { get; init; } = "serviceflow.notifications";
    public string RoutingKey { get; init; } = "request.#";
    public string DeadLetterExchange { get; init; } = "serviceflow.dead-letter";
    public string DeadLetterQueue { get; init; } = "serviceflow.notifications.dead-letter";
    public string DeadLetterRoutingKey { get; init; } = "serviceflow.notifications.failed";
    public ushort PrefetchCount { get; init; } = 10;
    public int ReconnectDelaySeconds { get; init; } = 5;
}
