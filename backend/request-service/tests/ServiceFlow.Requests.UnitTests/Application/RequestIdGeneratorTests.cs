using System.Collections.Concurrent;
using ServiceFlow.Requests.Application.Services;

namespace ServiceFlow.Requests.UnitTests.Application;

public sealed class RequestIdGeneratorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 22, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewId_GeneratesManyUniquePositiveJavaScriptSafeValues()
    {
        var generator = new RequestIdGenerator(new FixedTimeProvider(FixedNow), nodeId: 7);

        var ids = Enumerable.Range(0, 10_000).Select(_ => generator.NewId()).ToArray();

        Assert.All(ids, id => Assert.InRange(id, 1, RequestIdGenerator.MaxJavaScriptSafeInteger));
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.Equal(ids.Order(), ids);
    }

    [Fact]
    public void NewId_RemainsUniqueUnderConcurrency()
    {
        var generator = new RequestIdGenerator(new FixedTimeProvider(FixedNow), nodeId: 12);
        var ids = new ConcurrentBag<long>();

        Parallel.For(0, 20_000, _ => ids.Add(generator.NewId()));

        Assert.Equal(20_000, ids.Count);
        Assert.Equal(20_000, ids.Distinct().Count());
        Assert.All(ids, id => Assert.InRange(id, 1, RequestIdGenerator.MaxJavaScriptSafeInteger));
    }

    [Fact]
    public void NewId_UsesNodeBitsToAvoidCollisionsBetweenConfiguredNodes()
    {
        var timeProvider = new FixedTimeProvider(FixedNow);
        var firstNode = new RequestIdGenerator(timeProvider, nodeId: 1);
        var secondNode = new RequestIdGenerator(timeProvider, nodeId: 2);

        var firstIds = Enumerable.Range(0, 1_000).Select(_ => firstNode.NewId()).ToHashSet();
        var secondIds = Enumerable.Range(0, 1_000).Select(_ => secondNode.NewId()).ToArray();

        Assert.DoesNotContain(secondIds, firstIds.Contains);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(64)]
    public void Constructor_RejectsInvalidNode(int nodeId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RequestIdGenerator(new FixedTimeProvider(FixedNow), nodeId));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
