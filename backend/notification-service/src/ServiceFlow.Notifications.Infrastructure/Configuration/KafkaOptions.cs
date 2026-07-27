namespace ServiceFlow.Notifications.Infrastructure.Configuration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = "kafka:9092";
    public string Topic { get; init; } = "serviceflow.request-events";
    public string DeadLetterTopic { get; init; } = "serviceflow.request-events.dlq";
    public string GroupId { get; init; } = "serviceflow-notifications";
    public string ClientId { get; init; } = "serviceflow-notification-consumer";
    public int MaxProcessingAttempts { get; init; } = 2;
    public int RetryDelaySeconds { get; init; } = 2;
}
