using ServiceFlow.Requests.Application.Sla;
using ServiceFlow.Requests.Domain.Enums;

namespace ServiceFlow.Requests.UnitTests.Application;

public sealed class SlaStrategyTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(RequestPriority.Low, 168)]
    [InlineData(RequestPriority.Medium, 72)]
    [InlineData(RequestPriority.High, 24)]
    [InlineData(RequestPriority.Critical, 4)]
    public void Factory_SelectsStrategyForPriority(RequestPriority priority, int expectedHours)
    {
        var factory = new SlaStrategyFactory();

        var dueAt = factory.Create(priority).CalculateDueDate(CreatedAt);

        Assert.Equal(CreatedAt.AddHours(expectedHours), dueAt);
    }

    [Fact]
    public void Factory_RejectsUnknownPriority()
    {
        var factory = new SlaStrategyFactory();

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create((RequestPriority)999));
    }
}
