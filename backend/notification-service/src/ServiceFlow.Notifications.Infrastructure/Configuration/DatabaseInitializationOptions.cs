namespace ServiceFlow.Notifications.Infrastructure.Configuration;

public sealed class DatabaseInitializationOptions
{
    public const string SectionName = "DatabaseInitialization";

    public bool Enabled { get; init; } = true;
    public int MaxRetries { get; init; } = 15;
    public int RetryDelaySeconds { get; init; } = 2;
}
