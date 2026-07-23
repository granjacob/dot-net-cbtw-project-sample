using ServiceFlow.Notifications.Application.Contracts;

namespace ServiceFlow.Notifications.Application.Abstractions;

public interface INotificationRealtimePublisher
{
    Task PublishAsync(
        RequestEventEnvelope integrationEvent,
        NotificationDto notification,
        CancellationToken cancellationToken);
}
