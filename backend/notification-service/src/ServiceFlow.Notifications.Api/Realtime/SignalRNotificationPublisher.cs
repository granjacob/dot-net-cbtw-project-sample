using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using ServiceFlow.Notifications.Application.Abstractions;
using ServiceFlow.Notifications.Application.Contracts;

namespace ServiceFlow.Notifications.Api.Realtime;

internal sealed class SignalRNotificationPublisher(
    IHubContext<NotificationHub> hubContext,
    IOptions<RealtimeOptions> options) : INotificationRealtimePublisher
{
    public async Task PublishAsync(
        RequestEventEnvelope integrationEvent,
        NotificationDto notification,
        CancellationToken cancellationToken)
    {
        var clients = options.Value.BroadcastToAll
            ? hubContext.Clients.All
            : hubContext.Clients.Groups(
                UserIdentity.GroupName(notification.UserId),
                UserIdentity.RoleGroupName("Agent"),
                UserIdentity.RoleGroupName("Administrator"));

        await clients.SendAsync(
            integrationEvent.EventType,
            integrationEvent,
            cancellationToken);
        await clients.SendAsync(
            "NotificationCreated",
            notification,
            cancellationToken);
    }
}
