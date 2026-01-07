using Microsoft.Extensions.Logging;

namespace Flex.Infrastructures.Observability;

/// <summary>
/// Configuration options for global logging middleware.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Service name to include in logs
    /// </summary>
    public string ServiceName { get; set; } = "UnknownService";

    /// <summary>
    /// Enable request body logging (only for whitelisted paths)
    /// </summary>
    public bool EnableRequestBodyLogging { get; set; } = false;

    /// <summary>
    /// Enable response body logging (only for whitelisted paths)
    /// </summary>
    public bool EnableResponseBodyLogging { get; set; } = false;

    /// <summary>
    /// Maximum body size to log (in bytes) - default 10KB
    /// </summary>
    public int MaxBodySizeToLog { get; set; } = 10_240;

    /// <summary>
    /// Paths that are whitelisted for body logging (e.g., "/api/debug/*")
    /// </summary>
    public List<string> WhitelistedPaths { get; set; } = new();

    /// <summary>
    /// Paths to exclude from logging (e.g., "/health", "/metrics")
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new() 
    { 
        "/health", 
        "/healthz", 
        "/ready",
        "/metrics",
        "/swagger",
        "/favicon.ico"
    };

    /// <summary>
    /// Headers to exclude from logging (security)
    /// </summary>
    public List<string> ExcludedHeaders { get; set; } = new()
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key",
        "X-Auth-Token"
    };

    /// <summary>
    /// Enable IP address logging
    /// </summary>
    public bool EnableIpAddressLogging { get; set; } = true;

    /// <summary>
    /// Enable User-Agent logging (truncated)
    /// </summary>
    public bool EnableUserAgentLogging { get; set; } = true;

    /// <summary>
    /// Maximum User-Agent length to log
    /// </summary>
    public int MaxUserAgentLength { get; set; } = 200;

    /// <summary>
    /// Log level for successful requests (default: Information)
    /// </summary>
    public LogLevel SuccessLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Log level for client errors (4xx)
    /// </summary>
    public LogLevel ClientErrorLogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// Log level for server errors (5xx)
    /// </summary>
    public LogLevel ServerErrorLogLevel { get; set; } = LogLevel.Error;
}

