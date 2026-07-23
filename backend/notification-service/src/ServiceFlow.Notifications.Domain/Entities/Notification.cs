namespace ServiceFlow.Notifications.Domain.Entities;

public sealed class Notification
{
    private Notification()
    {
    }

    private Notification(
        Guid id,
        string userId,
        string type,
        string title,
        string message,
        DateTimeOffset createdAt,
        Guid eventId,
        long? requestId)
    {
        Id = id;
        UserId = userId;
        Type = type;
        Title = title;
        Message = message;
        CreatedAt = createdAt;
        EventId = eventId;
        RequestId = requestId;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid EventId { get; private set; }
    public long? RequestId { get; private set; }

    public static Notification Create(
        string userId,
        string type,
        string title,
        string message,
        Guid eventId,
        DateTimeOffset? createdAt = null,
        long? requestId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("The event id cannot be empty.", nameof(eventId));
        }

        return new Notification(
            Guid.NewGuid(),
            userId.Trim(),
            type.Trim(),
            title.Trim(),
            message.Trim(),
            createdAt ?? DateTimeOffset.UtcNow,
            eventId,
            requestId);
    }

    public bool MarkAsRead()
    {
        if (IsRead)
        {
            return false;
        }

        IsRead = true;
        return true;
    }
}
