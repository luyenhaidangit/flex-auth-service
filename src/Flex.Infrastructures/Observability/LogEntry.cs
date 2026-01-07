namespace Flex.Infrastructures.Observability;

/// <summary>
/// Standardized log entry for global logging across services.
/// Conforms to enterprise/banking logging standards.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Service name (e.g., "BranchService", "ApiGateway")
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method (GET, POST, etc.)
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Request path (e.g., "/api/branches")
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Request duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// User ID from context (if authenticated)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Client ID from context
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Client IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User-Agent header (truncated for security)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Request timestamp (ISO 8601)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Exception message (if error occurred)
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Request body (only for whitelisted paths in DEBUG mode)
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// Response body (only for whitelisted paths in DEBUG mode)
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

