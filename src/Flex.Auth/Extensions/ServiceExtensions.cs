using Flex.Infrastructures.Authentication;
using Flex.Infrastructures.EntityFrameworkCore;
using Flex.Infrastructures.Observability;
using Flex.Infrastructures.OpenApi;
using Flex.Infrastructures.Persistence;
using Flex.Infrastructures.RateLimiting;
using Flex.Infrastructures.Resilience;
using Flex.Infrastructures.Routing;

namespace Flex.Identity.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddConfigurationSettings(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOpenApi();
            services.AddEndpointsApiExplorer();
            services.ConfigureSwagger();

            // Global Logging
            services.AddGlobalLogging(configuration, serviceName: "IdentityService");

            // Customize
            services.AddRoutingConventions();

            // Gateway
            services.AddGatewayAuthentication();
            services.AddGatewayAuthorization();
            services.AddTrustedForwardedHeaders(configuration);
            services.AddGatewayRateLimiting();

            // Resilience (Timeout, Retry, Circuit Breaker, Bulkhead)
            services.AddHttpContextAccessor();
            services.AddTransient<CorrelationIdHandler>();
            services.AddDownstreamResilience(configuration);

            // Database
            services.ConfigureServiceDbContext<IdentityDbContext>(configuration, useWallet: true);

            // CORS
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            return services;
        }
    }
}
