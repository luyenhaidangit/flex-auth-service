using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Flex.Infrastructures.Routing
{
    public static class ForwardedHeadersExtensions
    {
        /// <summary>
        /// Configures forwarded headers for reverse proxy. Reads trusted IPs from 'TrustedIPs' config (use "*" to trust all).
        /// </summary>
        public static IServiceCollection AddTrustedForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();

                var trustedIPsValue = configuration["TrustedIPs"];

                if (trustedIPsValue == "*")
                {
                    return;
                }

                var proxyIps = configuration.GetSection("TrustedIPs").Get<string[]>();

                if (proxyIps != null)
                {
                    foreach (var ip in proxyIps)
                    {
                        options.KnownProxies.Add(IPAddress.Parse(ip));
                    }
                }
            });

            return services;
        }
    }
}
