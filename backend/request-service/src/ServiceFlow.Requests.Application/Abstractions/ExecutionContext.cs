namespace ServiceFlow.Requests.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface ICurrentUser
{
    string UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; }
}

public interface IRequestIdGenerator
{
    long NewId();
}
