namespace ServiceFlow.Notifications.Domain.Entities;

public sealed class ProcessedEvent
{
    private ProcessedEvent()
    {
    }

    private ProcessedEvent(Guid eventId, string eventType, DateTimeOffset processedAt)
    {
        EventId = eventId;
        EventType = eventType;
        ProcessedAt = processedAt;
    }

    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; private set; }

    public static ProcessedEvent Create(
        Guid eventId,
        string eventType,
        DateTimeOffset? processedAt = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("The event id cannot be empty.", nameof(eventId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        return new ProcessedEvent(eventId, eventType.Trim(), processedAt ?? DateTimeOffset.UtcNow);
    }
}
