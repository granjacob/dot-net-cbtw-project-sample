using ServiceFlow.Notifications.Domain.Entities;

namespace ServiceFlow.Notifications.UnitTests.Domain;

public sealed class ProcessedEventTests
{
    [Fact]
    public void Create_PreservesIdempotencyKeyAndEventType()
    {
        var eventId = Guid.NewGuid();
        var processedAt = DateTimeOffset.Parse("2026-07-22T12:00:00Z");

        var processedEvent = ProcessedEvent.Create(
            eventId,
            "RequestStatusChanged",
            processedAt);

        Assert.Equal(eventId, processedEvent.EventId);
        Assert.Equal("RequestStatusChanged", processedEvent.EventType);
        Assert.Equal(processedAt, processedEvent.ProcessedAt);
    }
}
