namespace Flex.Infrastructures.Observability;

/// <summary>
/// Temporary HTTP request logging context before values are pushed as ECS-compatible log properties.
/// </summary>
public class HttpLogContext
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public string? UserId { get; set; }
    public string? ClientId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
}
