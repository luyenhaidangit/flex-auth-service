using Microsoft.Extensions.Configuration;

namespace Flex.Infrastructures.Logging
{
    internal static class LogstashLoggingOptionsResolver
    {
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
