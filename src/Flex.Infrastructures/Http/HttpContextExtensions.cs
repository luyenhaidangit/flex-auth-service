using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.Http
{
    /// <summary>
    /// Extension methods for registering HTTP context services.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Adds HTTP context service to the service collection.
        /// </summary>
        public static IServiceCollection AddHttpContextService(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IHttpContextService, HttpContextService>();
            return services;
        }
    }
}
