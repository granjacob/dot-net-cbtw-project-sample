namespace ServiceFlow.Requests.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
    public string Exchange { get; init; } = "serviceflow.events";
    public string RoutingKey { get; init; } = "request.event";
    public string Queue { get; init; } = "serviceflow.notifications";
    public string BindingKey { get; init; } = "request.#";
    public string DeadLetterExchange { get; init; } = "serviceflow.dead-letter";
    public string DeadLetterQueue { get; init; } = "serviceflow.notifications.dead-letter";
    public string DeadLetterRoutingKey { get; init; } = "serviceflow.notifications.failed";
    public int PollingSeconds { get; init; } = 3;
    public int BatchSize { get; init; } = 50;
}
