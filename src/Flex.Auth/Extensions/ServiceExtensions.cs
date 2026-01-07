using Flex.Infrastructures.Authentication;
using Flex.Infrastructures.Observability;
using Flex.Infrastructures.OpenApi;
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
            services.AddGlobalLogging(configuration, serviceName: "ApiGateway");

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
