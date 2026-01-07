using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.Routing
{
    public static class RoutingExtensions
    {
        /// <summary>
        /// Configures routing options: lowercase URLs and query strings, no trailing slashes.
        /// </summary>
        public static IServiceCollection AddRoutingConventions(this IServiceCollection services)
        {
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
                options.AppendTrailingSlash = false;
            });

            return services;
        }
    }
}
