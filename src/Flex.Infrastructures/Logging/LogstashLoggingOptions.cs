using Microsoft.Extensions.Configuration;

namespace Flex.Infrastructures.Logging
{
    public sealed class LogstashLoggingOptions
    {
        public string Uri { get; set; } = string.Empty;
        public long QueueLimitBytes { get; set; } = 10_000_000;
        public int LogEventsInBatchLimit { get; set; } = 100;
        public int HealthCheckIntervalSeconds { get; set; } = 30;
        public int HealthCheckTimeoutSeconds { get; set; } = 3;

        public static LogstashLoggingOptions Resolve(IConfiguration configuration)
        {
            var loggingLogstash = configuration.GetSection("Logging:Logstash")
                .Get<LogstashLoggingOptions>();

            if (!string.IsNullOrWhiteSpace(loggingLogstash?.Uri))
            {
                return loggingLogstash;
            }

            return configuration.GetSection("Logstash")
                .Get<LogstashLoggingOptions>()
                ?? loggingLogstash
                ?? new LogstashLoggingOptions();
        }
    }
}
