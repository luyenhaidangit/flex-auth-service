using Flex.Infrastructures.Http;
using Microsoft.AspNetCore.Http;

namespace Flex.Infrastructures.Observability
{
    /// <summary>
    /// DelegatingHandler that propagates X-Correlation-Id to downstream services.
    /// Ensures trace correlation across all microservices.
    /// </summary>
    public class CorrelationIdHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CorrelationIdHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // Extract correlation ID from incoming request or use TraceIdentifier
            var correlationId =
                httpContext?.Request.Headers[HeaderNames.XCorrelationId].FirstOrDefault()
                ?? httpContext?.TraceIdentifier
                ?? Guid.NewGuid().ToString("N");

            // Propagate to downstream service
            request.Headers.TryAddWithoutValidation(HeaderNames.XCorrelationId, correlationId);

            return base.SendAsync(request, cancellationToken);
        }
    }
}