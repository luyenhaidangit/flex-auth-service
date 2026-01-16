using System.Security.Claims;

namespace Flex.Infrastructures.Http
{
    /// <summary>
    /// Accessor to access HTTP request context information such as IP address, user, headers, etc.
    /// </summary>
    public interface IRequestContextAccessor
    {
        /// <summary>
        /// Gets the client IP address from the request.
        /// First tries to get from X-Forwarded-For header (when behind API Gateway),
        /// then falls back to RemoteIpAddress.
        /// </summary>
        string? ClientIp { get; }

        /// <summary>
        /// Gets the current user (ClaimsPrincipal) from the HTTP context.
        /// </summary>
        ClaimsPrincipal? GetUser();

        /// <summary>
        /// Gets a specific claim value from the current user.
        /// </summary>
        string? GetClaimValue(string claimType);

        /// <summary>
        /// Gets a header value by name.
        /// </summary>
        string? GetHeader(string headerName);

        /// <summary>
        /// Gets the request scheme (HTTP/HTTPS).
        /// </summary>
        string? GetScheme();

        /// <summary>
        /// Gets the request path.
        /// </summary>
        string? GetPath();

        /// <summary>
        /// Gets the request query string.
        /// </summary>
        string? GetQueryString();

        /// <summary>
        /// Gets the HTTP method (GET, POST, etc.).
        /// </summary>
        string? GetMethod();

        /// <summary>
        /// Gets the correlation ID from headers or trace identifier.
        /// </summary>
        string? GetCorrelationId();

        /// <summary>
        /// Gets the request host.
        /// </summary>
        string? GetHost();

        /// <summary>
        /// Gets the full request URL.
        /// </summary>
        string? GetRequestUrl();
    }
}
