using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.Headers
{
    /// <summary>
    /// Extension methods for registering HTTP context and header services.
    /// </summary>
    public static class HeaderExtensions
    {
        /// <summary>
        /// Adds HTTP context service to the service collection.
        /// Provides easy access to HTTP context information such as IP address, user, headers, etc.
        /// </summary>
        public static IServiceCollection AddHttpContextService(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IHttpContextService, HttpContextService>();
            return services;
        }
    }
}
