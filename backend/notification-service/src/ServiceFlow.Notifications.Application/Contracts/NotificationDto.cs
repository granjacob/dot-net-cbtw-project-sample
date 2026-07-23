using ServiceFlow.Notifications.Domain.Entities;

namespace ServiceFlow.Notifications.Application.Contracts;

public sealed record NotificationDto(
    Guid Id,
    string UserId,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt,
    Guid EventId,
    long? RequestId)
{
    public static NotificationDto FromEntity(Notification notification) => new(
        notification.Id,
        notification.UserId,
        notification.Type,
        notification.Title,
        notification.Message,
        notification.IsRead,
        notification.CreatedAt,
        notification.EventId,
        notification.RequestId);
}
