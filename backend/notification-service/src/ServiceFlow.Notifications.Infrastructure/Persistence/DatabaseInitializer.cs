using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceFlow.Notifications.Infrastructure.Configuration;

namespace ServiceFlow.Notifications.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task EnsureCreatedWithRetryAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<DatabaseInitializationOptions>>()
            .Value;

        if (!options.Enabled)
        {
            return;
        }

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitialization");
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var maxRetries = Math.Max(1, options.MaxRetries);
        var retryDelay = TimeSpan.FromSeconds(Math.Max(1, options.RetryDelaySeconds));

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                logger.LogInformation("Notification database is ready.");
                return;
            }
            catch (Exception exception) when (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception,
                    "Notification database initialization attempt {Attempt}/{MaxRetries} failed. Retrying in {Delay}.",
                    attempt,
                    maxRetries,
                    retryDelay);
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        // The final attempt throws directly, so reaching this line is only possible after cancellation.
        cancellationToken.ThrowIfCancellationRequested();
    }
}
