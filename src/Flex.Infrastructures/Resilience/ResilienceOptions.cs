namespace Flex.Infrastructures.Resilience
{
    /// <summary>
    /// Configuration options for HTTP resilience patterns (Timeout, Retry, Circuit Breaker).
    /// </summary>
    public class ResilienceOptions
    {
        /// <summary>
        /// Total request timeout in seconds. This is the maximum time for the entire request including retries.
        /// Default: 30 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Attempt timeout in seconds. This is the timeout for each individual attempt (before retry).
        /// Must be less than TotalRequestTimeout. Default: 10 seconds.
        /// </summary>
        public int AttemptTimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Retry configuration.
        /// </summary>
        public RetryOptions Retry { get; set; } = new RetryOptions();

        /// <summary>
        /// Circuit breaker configuration.
        /// </summary>
        public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

        /// <summary>
        /// Concurrency limiter (bulkhead) configuration.
        /// </summary>
        public ConcurrencyLimiterOptions ConcurrencyLimiter { get; set; } = new();
    }

    /// <summary>
    /// Retry policy configuration.
    /// </summary>
    public class RetryOptions
    {
        /// <summary>
        /// Maximum number of retry attempts. Default: 1.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 1;

        /// <summary>
        /// Delay between retries in milliseconds. Default: 500ms.
        /// </summary>
        public int DelayMilliseconds { get; set; } = 500;

        /// <summary>
        /// Enable exponential backoff. Default: true.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Enable jitter to prevent thundering herd. Default: true.
        /// </summary>
        public bool UseJitter { get; set; } = true;
    }

    /// <summary>
    /// Circuit breaker configuration.
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Sampling duration in seconds. Default: 30 seconds.
        /// </summary>
        public int SamplingDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Minimum throughput (number of requests) to evaluate. Default: 20.
        /// </summary>
        public int MinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Failure ratio threshold (0.0 - 1.0). Circuit opens if failure ratio exceeds this value. Default: 0.5 (50%).
        /// </summary>
        public double FailureRatio { get; set; } = 0.5;

        /// <summary>
        /// Break duration in seconds. Default: 20 seconds.
        /// </summary>
        public int BreakDurationSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Concurrency limiter (bulkhead) configuration.
    /// </summary>
    public class ConcurrencyLimiterOptions
    {
        /// <summary>
        /// Maximum number of concurrent requests. Default: 200.
        /// </summary>
        public int PermitLimit { get; set; } = 200;

        /// <summary>
        /// Queue limit for waiting requests. Default: 0 (no queue, fail fast).
        /// </summary>
        public int QueueLimit { get; set; } = 0;
    }
}
