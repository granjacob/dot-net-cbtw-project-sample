using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Notifications.Infrastructure.Persistence;

namespace ServiceFlow.Notifications.Infrastructure.Health;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Notification database is reachable.")
                : HealthCheckResult.Unhealthy("Notification database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Notification database health check failed.",
                exception);
        }
    }
}
