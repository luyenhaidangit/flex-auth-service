using Flex.Infrastructures.Http;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Flex.Infrastructures.Http
{
    /// <summary>
    /// Implementation of IRequestContextAccessor to access HTTP context information.
    /// </summary>
    public class RequestContextAccessor : IRequestContextAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? ClientIp
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                    return null;

                // Get IP from X-Forwarded-For header (API Gateway forwards client IP here)
                var forwardedFor = httpContext.Request.Headers[HeaderNames.XForwardedFor].ToString();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    // X-Forwarded-For can contain multiple IPs: "client-ip, proxy1-ip, proxy2-ip"
                    // Take the first one (original client IP)
                    var firstIp = forwardedFor.Split(',')[0].Trim();
                    if (!string.IsNullOrEmpty(firstIp))
                        return firstIp;
                }

                // Fallback to RemoteIpAddress if X-Forwarded-For is not available
                return httpContext.Connection.RemoteIpAddress?.ToString();
            }
        }

        public ClaimsPrincipal? GetUser()
        {
            return _httpContextAccessor.HttpContext?.User;
        }

        public string? GetClaimValue(string claimType)
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
        }

        public string? GetHeader(string headerName)
        {
            if (string.IsNullOrEmpty(headerName))
                return null;

            return _httpContextAccessor.HttpContext?.Request.Headers[headerName].ToString();
        }

        public string? GetScheme()
        {
            return _httpContextAccessor.HttpContext?.Request.Scheme;
        }

        public string? GetPath()
        {
            return _httpContextAccessor.HttpContext?.Request.Path;
        }

        public string? GetQueryString()
        {
            return _httpContextAccessor.HttpContext?.Request.QueryString.ToString();
        }

        public string? GetMethod()
        {
            return _httpContextAccessor.HttpContext?.Request.Method;
        }

        public string? GetCorrelationId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            return httpContext.Request.Headers[HeaderNames.XCorrelationId].FirstOrDefault()
                ?? httpContext.TraceIdentifier;
        }

        public string? GetHost()
        {
            return _httpContextAccessor.HttpContext?.Request.Host.ToString();
        }

        public string? GetRequestUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return null;

            return $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
        }
    }
}
