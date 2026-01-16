using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Flex.Infrastructures.Observability
{
    public static class ObservabilityExtensions
    {
        /// <summary>
        /// Adds correlation ID middleware to propagate trace IDs across distributed services.
        /// </summary>
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }

        /// <summary>
        /// Adds global logging middleware for enterprise-grade request/response logging.
        /// Should be placed after CorrelationId middleware.
        /// </summary>
        public static IApplicationBuilder UseGlobalLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalLoggingMiddleware>();
        }

        /// <summary>
        /// Registers global logging services and configuration.
        /// </summary>
        public static IServiceCollection AddGlobalLogging(
            this IServiceCollection services, 
            IConfiguration configuration,
            string serviceName)
        {
            // Configure logging options
            services.Configure<LoggingOptions>(options =>
            {
                options.ServiceName = serviceName;
                
                // Bind from configuration if exists
                var loggingSection = configuration.GetSection("Logging:Global");
                if (loggingSection.Exists())
                {
                    loggingSection.Bind(options);
                }
            });

            return services;
        }

        /// <summary>
        /// Registers global logging services with custom options.
        /// </summary>
        public static IServiceCollection AddGlobalLogging(
            this IServiceCollection services,
            Action<LoggingOptions> configureOptions)
        {
            services.Configure(configureOptions);
            return services;
        }

    }
}
