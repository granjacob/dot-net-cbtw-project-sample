namespace ServiceFlow.Requests.Domain.Entities;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public string? CorrelationId { get; private set; }

    public static OutboxMessage Create(
        Guid id,
        string eventType,
        string payload,
        DateTimeOffset occurredAt,
        string? correlationId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Event id cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        return new OutboxMessage
        {
            Id = id,
            EventType = eventType.Trim(),
            Payload = payload,
            OccurredAt = occurredAt.ToUniversalTime(),
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim()
        };
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt.ToUniversalTime();
        Attempts++;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = string.IsNullOrWhiteSpace(error) ? "Unknown publishing error." : error[..Math.Min(error.Length, 2_000)];
    }
}
