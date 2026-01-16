using Flex.Infrastructures.Http;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace Flex.Infrastructures.Observability;

/// <summary>
/// Middleware that propagates trace ID as X-Correlation-Id header in both request and response for distributed tracing.
/// Also pushes CorrelationId to Serilog LogContext for automatic enrichment.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdPropertyName = "CorrelationId";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // Read from header or generate from Activity
        var correlationId = context.Request.Headers.TryGetValue(HeaderNames.XCorrelationId, out var headerValue)
            ? headerValue.ToString()
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

        // Set header for propagation (both request and response)
        context.Request.Headers[HeaderNames.XCorrelationId] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderNames.XCorrelationId] = correlationId;
            return Task.CompletedTask;
        });

        // Push to Serilog LogContext - automatic enrichment for ALL logs
        using (LogContext.PushProperty(CorrelationIdPropertyName, correlationId))
        {
            await _next(context);
        }
    }
}
