using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ServiceFlow.Notifications.Api.Realtime;

[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = UserIdentity.GetUserId(Context.User);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserIdentity.GroupName(userId));
        }

        foreach (var role in new[] { "Agent", "Administrator" })
        {
            if (Context.User?.IsInRole(role) == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, UserIdentity.RoleGroupName(role));
            }
        }

        await base.OnConnectedAsync();
    }
}
