using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using ServiceFlow.Requests.Domain.Entities;

namespace ServiceFlow.Requests.Infrastructure.Messaging;

internal static class KafkaOutboxMessageFactory
{
    public static Message<string, string> Create(OutboxMessage message) => new()
    {
        Key = GetPartitionKey(message),
        Value = message.Payload,
        Headers = new Headers
        {
            { "event-id", Encoding.UTF8.GetBytes(message.Id.ToString()) },
            { "event-type", Encoding.UTF8.GetBytes(message.EventType) },
            { "correlation-id", Encoding.UTF8.GetBytes(message.CorrelationId ?? string.Empty) },
            { "content-type", "application/json"u8.ToArray() }
        }
    };

    private static string GetPartitionKey(OutboxMessage message)
    {
        try
        {
            using var payload = JsonDocument.Parse(message.Payload);
            return payload.RootElement.TryGetProperty("requestId", out var requestId)
                ? requestId.ToString()
                : message.Id.ToString();
        }
        catch (JsonException)
        {
            return message.Id.ToString();
        }
    }
}
