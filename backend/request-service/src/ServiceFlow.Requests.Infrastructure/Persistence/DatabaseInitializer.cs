using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServiceFlow.Requests.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task EnsureDatabaseCreatedAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        const int maxAttempts = 12;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RequestDbContext>();
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                logger.LogInformation("Requests database is ready.");
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(attempt * 2, 10));
                logger.LogWarning(
                    exception,
                    "Requests database is not ready (attempt {Attempt}/{MaxAttempts}). Retrying in {DelaySeconds}s.",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
