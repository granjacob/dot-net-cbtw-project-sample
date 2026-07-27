namespace ServiceFlow.Requests.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = "localhost:29092";
    public string Topic { get; init; } = "serviceflow.request-events";
    public string ClientId { get; init; } = "serviceflow-request-outbox";
    public int PollingSeconds { get; init; } = 3;
    public int BatchSize { get; init; } = 50;
    public int MessageTimeoutSeconds { get; init; } = 10;
}
