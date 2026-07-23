namespace ServiceFlow.Requests.Api.Authentication;

public sealed record DemoUser(string Email, string Password, string Name, string Role);

public static class DemoUsers
{
    public const string EmployeeRole = "Employee";
    public const string AgentRole = "Agent";
    public const string AdministratorRole = "Administrator";

    public static readonly IReadOnlyList<DemoUser> All =
    [
        new("employee@serviceflow.local", "Employee123!", "Elena Employee", EmployeeRole),
        new("agent@serviceflow.local", "Agent123!", "Alex Agent", AgentRole),
        new("admin@serviceflow.local", "Admin123!", "Ada Administrator", AdministratorRole)
    ];

    public static DemoUser? Validate(string email, string password) => All.FirstOrDefault(user =>
        string.Equals(user.Email, email.Trim(), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(user.Password, password, StringComparison.Ordinal));
}
