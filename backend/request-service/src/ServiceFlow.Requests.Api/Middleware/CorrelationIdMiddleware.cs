using System.Diagnostics;
using ServiceFlow.Requests.Application.Abstractions;

namespace ServiceFlow.Requests.Api.Middleware;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string? CorrelationId { get; internal set; }
}

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context, CorrelationIdAccessor accessor)
    {
        var candidate = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = !string.IsNullOrWhiteSpace(candidate) && candidate.Length <= 128
            ? candidate
            : Guid.NewGuid().ToString("N");

        accessor.CorrelationId = correlationId;
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("serviceflow.correlation_id", correlationId);
        Activity.Current?.AddBaggage("correlation.id", correlationId);

        using var logScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId
        });
        await next(context);
    }
}
