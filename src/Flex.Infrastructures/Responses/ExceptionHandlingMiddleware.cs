using Flex.Infrastructures.Exceptions;
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

            var errorInfo = this.GetErrorInfo(ex);

            _logger.LogError(ex, "[{ErrorCode}] {Message}", errorInfo.ErrorCode, errorInfo.Message);
            await WriteErrorResponse(context, errorInfo.StatusCode, errorInfo.Message, errorInfo.ErrorCode, errorInfo.Errors);
        }

        private ErrorInfo GetErrorInfo(Exception ex)
        {
            // Authentication errors
            if (ex is SecurityTokenValidationException)
            {
                return new ErrorInfo
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    ErrorCode = ResponseCode.Unauthorized,
                    Message = "Invalid or expired token"
                };
            }

            if (ex is UnauthorizedAccessException)
            {
                return new ErrorInfo
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    ErrorCode = ResponseCode.Unauthorized,
                    Message = "Forbidden: You do not have permission"
                };
            }

            // Gateway Resilience errors
            if (ex is BrokenCircuitException)
            {
                return new ErrorInfo
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable,
                    ErrorCode = "CIRCUIT_BREAKER_OPEN",
                    Message = "Service temporarily unavailable. Circuit breaker is open."
                };
            }

            if (ex is TimeoutRejectedException)
            {
                return new ErrorInfo
                {
                    StatusCode = StatusCodes.Status504GatewayTimeout,
                    ErrorCode = "GATEWAY_TIMEOUT",
                    Message = "Request timeout. The downstream service did not respond in time."
                };
            }

            if (ex is HttpRequestException)
            {
                return new ErrorInfo
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable,
                    ErrorCode = "SERVICE_UNAVAILABLE",
                    Message = "Downstream service is unavailable"
                };
            }

            if (ex is ValidationException validationEx)
            {
                return new ErrorInfo
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ErrorCode = validationEx.ErrorCode
                };
            }

            // Default
            return new ErrorInfo
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorCode = ResponseCode.SystemError,
                Message = "An unexpected error occurred"
            };
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

        private async Task WriteErrorResponse(HttpContext context, int statusCode, string message, string errorCode, object? errors = null)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = Result.Failure(message: message, errorCode: errorCode, errors: errors);
            await context.Response.WriteAsJsonAsync(response, JsonOptions);
        }
    }
}
