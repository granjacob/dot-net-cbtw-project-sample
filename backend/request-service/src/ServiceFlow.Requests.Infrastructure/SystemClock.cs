using ServiceFlow.Requests.Application.Abstractions;

namespace ServiceFlow.Requests.Infrastructure;

public sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
