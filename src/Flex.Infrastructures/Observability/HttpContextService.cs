using Flex.Infrastructures.Headers;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Flex.Infrastructures.Observability
{
    /// <summary>
    /// Implementation of IHttpContextService to access HTTP context information.
    /// </summary>
    public class HttpContextService : IHttpContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }

        public ClaimsPrincipal? GetUser()
        {
            return _httpContextAccessor.HttpContext?.User;
        }

        public string? GetClaimValue(string claimType)
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
        }

        public string? GetUserAgent()
        {
            return GetHeader(HeaderNames.UserAgent);
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
