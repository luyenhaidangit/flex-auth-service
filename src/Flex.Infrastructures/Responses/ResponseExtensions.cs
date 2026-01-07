using Microsoft.AspNetCore.Builder;

namespace Flex.Infrastructures.Responses
{
    public static class ResponseExtensions
    {
        /// <summary>
        /// Adds global exception handling middleware for standardized error responses.
        /// </summary>
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }

        /// <summary>
        /// Adds request validation middleware to enforce Content-Type requirements for POST/PUT/PATCH requests.
        /// </summary>
        public static IApplicationBuilder UseRequestGuard(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestGuardMiddleware>();
        }
    }
}
