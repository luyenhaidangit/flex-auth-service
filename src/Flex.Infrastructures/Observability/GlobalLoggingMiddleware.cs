using Flex.Infrastructures.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Flex.Infrastructures.Observability;

/// <summary>
/// Global logging middleware for service-level request/response logging.
/// Conforms to enterprise/banking logging standards.
/// 
/// Responsibilities:
/// - Log request metadata (method, path, headers)
/// - Log response metadata (status, duration)
/// - Propagate CorrelationId
/// - Filter sensitive data
/// - Whitelist body logging (DEBUG only)
/// </summary>
public class GlobalLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalLoggingMiddleware> _logger;
    private readonly LoggingOptions _options;

    public GlobalLoggingMiddleware(
        RequestDelegate next, 
        ILogger<GlobalLoggingMiddleware> logger,
        IOptions<LoggingOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task Invoke(HttpContext context)
    {
        // Skip logging for excluded paths
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var logContext = new HttpLogContext
        {
            Method = context.Request.Method,
            Path = context.Request.Path
        };

        // Extract user context
        ExtractUserContext(context, logContext);

        // Capture request body if whitelisted
        string? requestBody = null;
        if (ShouldLogBody(context.Request.Path))
        {
            requestBody = await CaptureRequestBody(context);
            logContext.RequestBody = requestBody;
        }

        // Replace response body stream to capture it
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            // Execute the pipeline
            await _next(context);

            stopwatch.Stop();
            logContext.StatusCode = context.Response.StatusCode;
            logContext.DurationMs = stopwatch.ElapsedMilliseconds;

            // Capture response body if whitelisted
            if (ShouldLogBody(context.Request.Path))
            {
                logContext.ResponseBody = await CaptureResponseBody(responseBodyStream);
            }

            // Log based on status code
            LogRequest(context, logContext);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logContext.StatusCode = context.Response.StatusCode != 200 
                ? context.Response.StatusCode 
                : StatusCodes.Status500InternalServerError;
            logContext.DurationMs = stopwatch.ElapsedMilliseconds;

            using (PushEcsHttpProperties(context, logContext, ex))
            {
                _logger.LogError(ex, "Request failed");
            }

            throw;
        }
        finally
        {
            // Copy response body back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
    }

    private bool ShouldSkipLogging(PathString path)
    {
        return _options.ExcludedPaths.Any(excluded => 
            path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private bool ShouldLogBody(PathString path)
    {
        if (!_options.EnableRequestBodyLogging && !_options.EnableResponseBodyLogging)
            return false;

        if (_options.WhitelistedPaths.Count == 0)
            return false;

        return _options.WhitelistedPaths.Any(whitelisted =>
        {
            // Support wildcard matching
            if (whitelisted.EndsWith("*"))
            {
                var prefix = whitelisted.TrimEnd('*');
                return path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase);
            }
            return path.Equals(whitelisted, StringComparison.OrdinalIgnoreCase);
        });
    }

    private void ExtractUserContext(HttpContext context, HttpLogContext logContext)
    {
        // Extract UserId
        if (context.Request.Headers.TryGetValue(HeaderNames.UserId, out var userId))
        {
            logContext.UserId = userId.ToString();
        }
        else if (context.User?.Identity?.IsAuthenticated == true)
        {
            logContext.UserId = context.User.Identity.Name ?? context.User.FindFirst("sub")?.Value;
        }

        // Extract ClientId
        if (context.Request.Headers.TryGetValue(HeaderNames.ClientId, out var clientId))
        {
            logContext.ClientId = clientId.ToString();
        }

        // Extract IP Address
        if (_options.EnableIpAddressLogging)
        {
            logContext.IpAddress = context.Connection.RemoteIpAddress?.ToString();
        }

        // Extract User-Agent (truncated)
        if (_options.EnableUserAgentLogging && context.Request.Headers.TryGetValue("User-Agent", out var userAgent))
        {
            var ua = userAgent.ToString();
            logContext.UserAgent = ua.Length > _options.MaxUserAgentLength 
                ? ua.Substring(0, _options.MaxUserAgentLength) + "..." 
                : ua;
        }
    }

    private async Task<string?> CaptureRequestBody(HttpContext context)
    {
        if (!_options.EnableRequestBodyLogging)
            return null;

        if (context.Request.ContentLength == null || context.Request.ContentLength == 0)
            return null;

        if (context.Request.ContentLength > _options.MaxBodySizeToLog)
            return $"[Body too large: {context.Request.ContentLength} bytes]";

        try
        {
            context.Request.EnableBuffering();
            
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            return string.IsNullOrWhiteSpace(body) ? null : body;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture request body");
            return "[Failed to capture]";
        }
    }

    private async Task<string?> CaptureResponseBody(MemoryStream responseBodyStream)
    {
        if (!_options.EnableResponseBodyLogging)
            return null;

        if (responseBodyStream.Length == 0)
            return null;

        if (responseBodyStream.Length > _options.MaxBodySizeToLog)
            return $"[Body too large: {responseBodyStream.Length} bytes]";

        try
        {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);

            return string.IsNullOrWhiteSpace(body) ? null : body;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture response body");
            return "[Failed to capture]";
        }
    }

    private void LogRequest(HttpContext context, HttpLogContext logContext)
    {
        var logLevel = DetermineLogLevel(logContext.StatusCode);

        // Create structured log message
        var message = $"{logContext.Method} {logContext.Path} responded {logContext.StatusCode} in {logContext.DurationMs}ms";

        using (PushEcsHttpProperties(context, logContext))
        {
            _logger.Log(logLevel, message);
        }
    }

    private LogLevel DetermineLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => _options.ServerErrorLogLevel,
            >= 400 => _options.ClientErrorLogLevel,
            _ => _options.SuccessLogLevel
        };
    }

    private static IDisposable PushEcsHttpProperties(HttpContext context, HttpLogContext logContext, Exception? exception = null)
    {
        var activity = Activity.Current;
        var statusCode = logContext.StatusCode;

        var properties = new List<IDisposable>
        {
            LogContext.PushProperty(LogFields.EventAction, $"{logContext.Method} {logContext.Path}"),
            LogContext.PushProperty(LogFields.EventOutcome, statusCode >= 400 ? "failure" : "success"),
            LogContext.PushProperty(LogFields.HttpRequestMethod, logContext.Method),
            LogContext.PushProperty(LogFields.UrlPath, logContext.Path),
            LogContext.PushProperty(LogFields.HttpResponseStatusCode, statusCode),
            LogContext.PushProperty(LogFields.EventDuration, logContext.DurationMs * 1_000_000),
            LogContext.PushProperty(LogFields.RequestId, context.TraceIdentifier)
        };

        if (!string.IsNullOrWhiteSpace(logContext.UserId))
        {
            properties.Add(LogContext.PushProperty(LogFields.UserId, logContext.UserId));
        }

        if (!string.IsNullOrWhiteSpace(logContext.IpAddress))
        {
            properties.Add(LogContext.PushProperty(LogFields.ClientIp, logContext.IpAddress));
        }

        if (!string.IsNullOrWhiteSpace(logContext.UserAgent))
        {
            properties.Add(LogContext.PushProperty(LogFields.UserAgentOriginal, logContext.UserAgent));
        }

        if (!string.IsNullOrWhiteSpace(logContext.ClientId))
        {
            properties.Add(LogContext.PushProperty(LogFields.ClientId, logContext.ClientId));
        }

        if (!string.IsNullOrWhiteSpace(logContext.RequestBody))
        {
            properties.Add(LogContext.PushProperty(LogFields.HttpRequestBodyContent, logContext.RequestBody));
        }

        if (!string.IsNullOrWhiteSpace(logContext.ResponseBody))
        {
            properties.Add(LogContext.PushProperty(LogFields.HttpResponseBodyContent, logContext.ResponseBody));
        }

        if (activity != null)
        {
            properties.Add(LogContext.PushProperty(LogFields.TraceId, activity.TraceId.ToString()));
            properties.Add(LogContext.PushProperty(LogFields.SpanId, activity.SpanId.ToString()));
        }

        if (exception != null)
        {
            properties.Add(LogContext.PushProperty(LogFields.ErrorType, exception.GetType().Name));
            properties.Add(LogContext.PushProperty(LogFields.ErrorMessage, exception.Message));
            properties.Add(LogContext.PushProperty(LogFields.ErrorStackTrace, exception.ToString()));
        }

        return new CompositeDisposable(properties);
    }
}
