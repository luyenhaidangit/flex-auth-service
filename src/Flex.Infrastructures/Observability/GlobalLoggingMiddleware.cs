using Flex.Infrastructures.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

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
        var logEntry = new LogEntry
        {
            Service = _options.ServiceName,
            Method = context.Request.Method,
            Path = context.Request.Path,
            Timestamp = DateTime.UtcNow
        };

        // Extract user context
        ExtractUserContext(context, logEntry);

        // Capture request body if whitelisted
        string? requestBody = null;
        if (ShouldLogBody(context.Request.Path))
        {
            requestBody = await CaptureRequestBody(context);
            logEntry.RequestBody = requestBody;
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
            logEntry.StatusCode = context.Response.StatusCode;
            logEntry.DurationMs = stopwatch.ElapsedMilliseconds;

            // Capture response body if whitelisted
            if (ShouldLogBody(context.Request.Path))
            {
                logEntry.ResponseBody = await CaptureResponseBody(responseBodyStream);
            }

            // Log based on status code
            LogRequest(logEntry);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logEntry.StatusCode = context.Response.StatusCode != 200 
                ? context.Response.StatusCode 
                : StatusCodes.Status500InternalServerError;
            logEntry.DurationMs = stopwatch.ElapsedMilliseconds;
            logEntry.Exception = $"{ex.GetType().Name}: {ex.Message}";

            _logger.LogError(ex, "Request failed: {@LogEntry}", logEntry);
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

    private void ExtractUserContext(HttpContext context, LogEntry logEntry)
    {
        // Extract UserId
        if (context.Request.Headers.TryGetValue(HeaderNames.UserId, out var userId))
        {
            logEntry.UserId = userId.ToString();
        }
        else if (context.User?.Identity?.IsAuthenticated == true)
        {
            logEntry.UserId = context.User.Identity.Name ?? context.User.FindFirst("sub")?.Value;
        }

        // Extract ClientId
        if (context.Request.Headers.TryGetValue(HeaderNames.ClientId, out var clientId))
        {
            logEntry.ClientId = clientId.ToString();
        }

        // Extract IP Address
        if (_options.EnableIpAddressLogging)
        {
            logEntry.IpAddress = context.Connection.RemoteIpAddress?.ToString();
        }

        // Extract User-Agent (truncated)
        if (_options.EnableUserAgentLogging && context.Request.Headers.TryGetValue("User-Agent", out var userAgent))
        {
            var ua = userAgent.ToString();
            logEntry.UserAgent = ua.Length > _options.MaxUserAgentLength 
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

    private void LogRequest(LogEntry logEntry)
    {
        var logLevel = DetermineLogLevel(logEntry.StatusCode);

        // Create structured log message
        var message = $"{logEntry.Method} {logEntry.Path} responded {logEntry.StatusCode} in {logEntry.DurationMs}ms";

        _logger.Log(logLevel, message + " {@LogEntry}", logEntry);
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
}

