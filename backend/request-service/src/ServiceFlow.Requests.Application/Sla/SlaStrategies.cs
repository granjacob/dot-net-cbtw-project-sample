using ServiceFlow.Requests.Domain.Enums;

namespace ServiceFlow.Requests.Application.Sla;

public interface ISlaStrategy
{
    DateTimeOffset CalculateDueDate(DateTimeOffset createdAt);
}

public sealed class LowPrioritySlaStrategy : ISlaStrategy
{
    public DateTimeOffset CalculateDueDate(DateTimeOffset createdAt) => createdAt.AddDays(7);
}

public sealed class MediumPrioritySlaStrategy : ISlaStrategy
{
    public DateTimeOffset CalculateDueDate(DateTimeOffset createdAt) => createdAt.AddDays(3);
}

public sealed class HighPrioritySlaStrategy : ISlaStrategy
{
    public DateTimeOffset CalculateDueDate(DateTimeOffset createdAt) => createdAt.AddDays(1);
}

public sealed class CriticalPrioritySlaStrategy : ISlaStrategy
{
    public DateTimeOffset CalculateDueDate(DateTimeOffset createdAt) => createdAt.AddHours(4);
}

public interface ISlaStrategyFactory
{
    ISlaStrategy Create(RequestPriority priority);
}

public sealed class SlaStrategyFactory : ISlaStrategyFactory
{
    private static readonly ISlaStrategy Low = new LowPrioritySlaStrategy();
    private static readonly ISlaStrategy Medium = new MediumPrioritySlaStrategy();
    private static readonly ISlaStrategy High = new HighPrioritySlaStrategy();
    private static readonly ISlaStrategy Critical = new CriticalPrioritySlaStrategy();

    public ISlaStrategy Create(RequestPriority priority) => priority switch
    {
        RequestPriority.Low => Low,
        RequestPriority.Medium => Medium,
        RequestPriority.High => High,
        RequestPriority.Critical => Critical,
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unsupported request priority.")
    };
}
