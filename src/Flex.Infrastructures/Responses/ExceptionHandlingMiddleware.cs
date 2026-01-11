using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Text.Json;

namespace Flex.Infrastructures.Responses
{
    /// <summary>
    /// Middleware that handles unhandled exceptions and error status codes, returning standardized JSON error responses.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);

                if (!context.Response.HasStarted && this.IsErrorStatusCode(context.Response.StatusCode))
                {
                    await HandleCustomStatusCode(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await this.HandleException(context, ex);
            }
        }

        private async Task HandleException(HttpContext context, Exception ex)
        {
            if (context.Response.HasStarted) return;

            var (statusCode, errorCode, message) = ex switch
            {
                // Authentication errors
                SecurityTokenValidationException => 
                    (StatusCodes.Status401Unauthorized, ResponseCode.Unauthorized, "Invalid or expired token"),
                UnauthorizedAccessException => 
                    (StatusCodes.Status403Forbidden, ResponseCode.Unauthorized, "Forbidden: You do not have permission"),

                // Gateway Resilience errors
                BrokenCircuitException => 
                    (StatusCodes.Status503ServiceUnavailable, "CIRCUIT_BREAKER_OPEN", "Service temporarily unavailable. Circuit breaker is open."),
                TimeoutRejectedException => 
                    (StatusCodes.Status504GatewayTimeout, "GATEWAY_TIMEOUT", "Request timeout. The downstream service did not respond in time."),
                HttpRequestException => 
                    (StatusCodes.Status503ServiceUnavailable, "SERVICE_UNAVAILABLE", "Downstream service is unavailable"),

                // Default
                _ => (StatusCodes.Status500InternalServerError, ResponseCode.SystemError, "An unexpected error occurred")
            };

            _logger.LogError(ex, "[{ErrorCode}] {Message}", errorCode, message);
            await WriteErrorResponse(context, statusCode, message, errorCode);
        }

        private bool IsErrorStatusCode(int statusCode) => statusCode >= 400 && statusCode < 600;

        private async Task HandleCustomStatusCode(HttpContext context)
        {
            var (message, errorCode) = context.Response.StatusCode switch
            {
                StatusCodes.Status401Unauthorized => ("Unauthorized Access. Please provide a valid token.", ResponseCode.Unauthorized),
                StatusCodes.Status403Forbidden => ("Forbidden: You do not have permission.", ResponseCode.Forbidden),
                StatusCodes.Status404NotFound => ("API Not Found.", ResponseCode.NotFound),
                _ => ("An unexpected error occurred.", ResponseCode.SystemError)
            };

            await WriteErrorResponse(context, context.Response.StatusCode, message, errorCode);
        }

        private async Task WriteErrorResponse(HttpContext context, int statusCode, string message, string errorCode)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = Result.Failure(message: message, errorCode: errorCode);
            await context.Response.WriteAsJsonAsync(response, JsonOptions);
        }
    }
}
