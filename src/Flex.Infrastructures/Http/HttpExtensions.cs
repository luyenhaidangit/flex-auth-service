using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.Http
{
    /// <summary>
    /// Extension methods for registering HTTP context services.
    /// </summary>
    public static class HttpExtensions
    {
        /// <summary>
        /// Adds request context accessor to the service collection.
        /// Provides easy access to HTTP context information such as IP address, user, headers, etc.
        /// </summary>
        public static IServiceCollection AddRequestContextAccessor(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IRequestContextAccessor, RequestContextAccessor>();
            return services;
        }
    }
}
