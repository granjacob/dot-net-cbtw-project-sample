using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceFlow.Notifications.Application.Contracts;

public sealed class RequestEventEnvelope
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; }
    public long RequestId { get; init; }
    public string? UserId { get; init; }
    public string? Title { get; init; }
    public string? Message { get; init; }
    public string? CorrelationId { get; init; }
    public JsonElement? Data { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}
