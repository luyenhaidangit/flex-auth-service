using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Flex.Infrastructures.Responses
{
    /// <summary>
    /// Middleware that validates Content-Type header for POST/PUT/PATCH requests, enforcing application/json requirement.
    /// </summary>
    public class RequestGuardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestGuardMiddleware> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public RequestGuardMiddleware(RequestDelegate next, ILogger<RequestGuardMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (this.IsInvalidContentType(context))
            {
                _logger.LogWarning("Request {TraceId} rejected: Missing Content-Type for {Method} {Path}",
                context.TraceIdentifier, context.Request.Method, context.Request.Path);

                await this.HandleCustomResponseAsync(context, 
                    StatusCodes.Status415UnsupportedMediaType, 
                    ResponseCode.InvalidContentType);

                return;
            }

            await _next(context);
        }

        private bool IsInvalidContentType(HttpContext context)
        {
            if (context.Request.Method == HttpMethods.Post ||
                context.Request.Method == HttpMethods.Put ||
                context.Request.Method == HttpMethods.Patch)
            {
                if (!context.Request.ContentLength.HasValue || context.Request.ContentLength == 0)
                    return false;

                if (string.IsNullOrWhiteSpace(context.Request.ContentType))
                    return true;

                return !context.Request.ContentType.StartsWith("application/json",StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private async Task HandleCustomResponseAsync(HttpContext context, int statusCode, string responseCode)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = ApiResponse.Failure(errorCode: responseCode);
            var responseJson = JsonSerializer.Serialize(response, JsonOptions);
            await context.Response.WriteAsync(responseJson);
        }
    }
}
