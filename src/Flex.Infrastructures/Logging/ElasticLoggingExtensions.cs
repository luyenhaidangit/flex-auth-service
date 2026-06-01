using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.Logging
{
    public static class ElasticLoggingExtensions
    {
        internal const string HttpClientName = "elastic-logging-monitor";

        public static IServiceCollection AddElasticLoggingConnectionMonitor(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var sinks = LoggingSinkOptions.Resolve(configuration);
            var options = ElasticLoggingOptionsResolver.Resolve(configuration);

            if (!sinks.Elastic || string.IsNullOrWhiteSpace(options.NodeUris))
            {
                return services;
            }

            services.AddSingleton(options);

            services.AddHttpClient(HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.HealthCheckTimeoutSeconds));
            }).RemoveAllLoggers();

            services.AddHostedService<ElasticLoggingConnectionMonitor>();

            return services;
        }

        public static IServiceCollection AddLogstashLoggingConnectionMonitor(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var sinks = LoggingSinkOptions.Resolve(configuration);
            var options = LogstashLoggingOptionsResolver.Resolve(configuration);

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
