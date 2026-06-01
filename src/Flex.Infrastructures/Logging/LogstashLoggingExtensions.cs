using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.Logging
{
    public static class LogstashLoggingExtensions
    {
        public static IServiceCollection AddLogstashLoggingConnectionMonitor(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var sinks = LoggingSinkOptions.Resolve(configuration);
            var options = LogstashLoggingOptions.Resolve(configuration);

            if (!sinks.Logstash || string.IsNullOrWhiteSpace(options.Uri))
            {
                return services;
            }

            services.AddSingleton(options);
            services.AddHostedService<LogstashConnectionMonitor>();

            return services;
        }
    }
}
