using ServiceFlow.Requests.Domain.Entities;

namespace ServiceFlow.Requests.UnitTests.Domain;

public sealed class OutboxMessageTests
{
    [Fact]
    public void PublishingState_TracksFailuresAndSuccess()
    {
        var now = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
        var message = OutboxMessage.Create(Guid.NewGuid(), "RequestCreated", "{}", now, "correlation");

        message.MarkFailed("Broker unavailable");
        message.MarkProcessed(now.AddMinutes(1));

        Assert.Equal(2, message.Attempts);
        Assert.Null(message.LastError);
        Assert.Equal(now.AddMinutes(1), message.ProcessedAt);
    }
}
