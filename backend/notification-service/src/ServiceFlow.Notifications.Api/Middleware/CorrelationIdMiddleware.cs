using System.Diagnostics;

namespace ServiceFlow.Notifications.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context.Request);
        context.TraceIdentifier = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await next(context);
        }
    }

    private static string GetCorrelationId(HttpRequest request)
    {
        var supplied = request.Headers[HeaderName].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(supplied) && supplied.Length <= 128
            ? supplied
            : Guid.NewGuid().ToString("N");
    }
}
