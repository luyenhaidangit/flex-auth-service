namespace Flex.Infrastructures.Logging
{
    public class ElasticLoggingOptions
    {
        public bool Enabled { get; set; } = true;
        public string NodeUris { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string IndexPrefix { get; set; } = "logs";
        public int HealthCheckIntervalSeconds { get; set; } = 60;
        public int HealthCheckTimeoutSeconds { get; set; } = 5;
    }
}
