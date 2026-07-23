using System.Security.Claims;
using ServiceFlow.Requests.Application.Abstractions;

namespace ServiceFlow.Requests.Api.Authentication;

public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public string UserId => Principal?.FindFirstValue("sub")
        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? string.Empty;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}

public static class AuthorizationPolicies
{
    public const string CreateRequests = "CreateRequests";
    public const string MutateRequests = "MutateRequests";
}
