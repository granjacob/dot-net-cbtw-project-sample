using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServiceFlow.Requests.Infrastructure.Persistence;

namespace ServiceFlow.Requests.Infrastructure.Health;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RequestDbContext>();
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Requests database is reachable.")
                : HealthCheckResult.Unhealthy("Requests database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Requests database health check failed.", exception);
        }
    }
}
