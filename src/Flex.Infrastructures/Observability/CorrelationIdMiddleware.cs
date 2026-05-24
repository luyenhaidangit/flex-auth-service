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
    private const string LabelsCorrelationIdPropertyName = "labels.correlation_id";
    private const string TraceIdPropertyName = "trace.id";
    private const string TransactionIdPropertyName = "transaction.id";
    private const string SpanIdPropertyName = "span.id";

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

        var activity = Activity.Current;
        var traceId = activity?.TraceId.ToString() ?? correlationId;
        var spanId = activity?.SpanId.ToString();

        // Push both legacy and ECS-compatible properties for all logs in the request.
        using (LogContext.PushProperty(CorrelationIdPropertyName, correlationId))
        using (LogContext.PushProperty(LabelsCorrelationIdPropertyName, correlationId))
        using (LogContext.PushProperty(TraceIdPropertyName, traceId))
        using (LogContext.PushProperty(TransactionIdPropertyName, context.TraceIdentifier))
        using (LogContext.PushProperty(SpanIdPropertyName, spanId))
        {
            await _next(context);
        }
    }
}
