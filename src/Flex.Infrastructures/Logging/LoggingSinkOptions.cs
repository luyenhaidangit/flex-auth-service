using Microsoft.Extensions.Configuration;

namespace Flex.Infrastructures.Logging
{
    internal sealed class LoggingSinkOptions
    {
        public bool Console { get; set; } = true;
        public bool File { get; set; }
        public bool Logstash { get; set; }
        public bool Elastic { get; set; }

        public static LoggingSinkOptions Resolve(IConfiguration configuration)
        {
            return configuration.GetSection("Logging:Sinks")
                .Get<LoggingSinkOptions>()
                ?? new LoggingSinkOptions();
        }
    }
}
