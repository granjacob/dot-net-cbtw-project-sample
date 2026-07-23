using Microsoft.EntityFrameworkCore;
using ServiceFlow.Notifications.Domain.Entities;

namespace ServiceFlow.Notifications.Infrastructure.Persistence;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options)
    : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
    }
}
