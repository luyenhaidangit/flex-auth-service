namespace Flex.Infrastructures.Logging
{
    public sealed class LogstashLoggingOptions
    {
        public bool Enabled { get; set; }
        public string Uri { get; set; } = string.Empty;
        public int QueueCapacity { get; set; } = 10000;
    }
}
