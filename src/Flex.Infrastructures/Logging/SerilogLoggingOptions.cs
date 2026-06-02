using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Flex.Infrastructures.Logging
{
    public sealed class SerilogLoggingOptions
    {
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceEnvironment { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public LoggingSinkOptions Sinks { get; set; } = new();
        public LogstashLoggingOptions Logstash { get; set; } = new();

        public static SerilogLoggingOptions Resolve(IConfiguration configuration, IWebHostEnvironment environment)
        {
            return new SerilogLoggingOptions
            {
                ServiceName = ResolveServiceName(configuration, environment),
                ServiceEnvironment = ResolveServiceEnvironment(environment),
                HostName = Environment.MachineName,
                Sinks = LoggingSinkOptions.Resolve(configuration),
                Logstash = LogstashLoggingOptions.Resolve(configuration)
            };
        }

        private static string ResolveServiceName(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var configuredServiceName = configuration["Logging:ServiceName"];
            if (!string.IsNullOrWhiteSpace(configuredServiceName))
            {
                return configuredServiceName.Trim();
            }

            return environment.ApplicationName?.ToLowerInvariant().Replace('.', '-') ?? "unknown-application";
        }

        private static string ResolveServiceEnvironment(IWebHostEnvironment environment)
        {
            return string.IsNullOrWhiteSpace(environment.EnvironmentName)
                ? "development"
                : environment.EnvironmentName.Trim().ToLowerInvariant();
        }
    }
}
