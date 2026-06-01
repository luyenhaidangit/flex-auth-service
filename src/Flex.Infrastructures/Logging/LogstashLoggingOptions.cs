namespace Flex.Infrastructures.Logging
{
    public sealed class LogstashLoggingOptions
    {
        public string Uri { get; set; } = string.Empty;
        public int QueueCapacity { get; set; } = 10000;
        public int HealthCheckIntervalSeconds { get; set; } = 30;
        public int HealthCheckTimeoutSeconds { get; set; } = 3;
    }
}
