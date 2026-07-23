using System.Security.Claims;

namespace ServiceFlow.Notifications.Api.Realtime;

internal static class UserIdentity
{
    public const string DemoUserId = "employee@serviceflow.local";

    public static string GetUserId(ClaimsPrincipal? user) =>
        user?.FindFirstValue("sub")
        ?? user?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user?.FindFirstValue("email")
        ?? user?.Identity?.Name
        ?? DemoUserId;

    public static string GroupName(string userId) => $"user:{userId.Trim().ToLowerInvariant()}";

    public static string RoleGroupName(string role) => $"role:{role.Trim().ToLowerInvariant()}";
}
