using Microsoft.EntityFrameworkCore;
using ServiceFlow.Notifications.Domain.Entities;
using ServiceFlow.Notifications.Infrastructure.Persistence;

namespace ServiceFlow.Notifications.UnitTests.Infrastructure;

public sealed class IdempotencyModelTests
{
    [Fact]
    public void EfModel_EnforcesEventIdIdempotency()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseSqlServer("Server=unused;Database=unused;User Id=unused;Password=unused")
            .Options;
        using var dbContext = new NotificationDbContext(options);

        var notificationType = dbContext.Model.FindEntityType(typeof(Notification));
        var eventIndex = Assert.Single(
            notificationType!.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Notification.EventId)]));
        var processedEventType = dbContext.Model.FindEntityType(typeof(ProcessedEvent));

        Assert.True(eventIndex.IsUnique);
        Assert.Equal(
            nameof(ProcessedEvent.EventId),
            Assert.Single(processedEventType!.FindPrimaryKey()!.Properties).Name);
    }
}
