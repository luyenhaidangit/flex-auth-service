using Flex.Infrastructures.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Timeout;
using System.Net;

namespace Flex.Infrastructures.Resilience
{
    /// <summary>
    /// Extension methods for configuring HTTP resilience patterns (Timeout, Retry, Circuit Breaker, Bulkhead).
    /// Production-ready configuration to prevent cascade failures and improve gateway stability.
    /// </summary>
    public static class ResilienceExtensions
    {
        public const string DownstreamResilient = "downstream-resilient";

        /// <summary>
        /// Adds standard resilience pipeline for downstream HTTP calls.
        /// Configuration is loaded from "Resilience" section in appsettings.json.
        /// Includes: Total Request Timeout, Limited Retry, Circuit Breaker, Concurrency Limiter.
        /// </summary>
        public static IServiceCollection AddDownstreamResilience(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind resilience options from configuration
            var resilienceOptions = configuration.GetSection("Resilience")
                .Get<ResilienceOptions>()
                ?? new ResilienceOptions();

            services.AddHttpClient(DownstreamResilient)
                .AddHttpMessageHandler<CorrelationIdHandler>()
                .AddStandardResilienceHandler(options =>
                {
                    var totalRequestTimeout = TimeSpan.FromSeconds(Math.Max(1, resilienceOptions.TimeoutSeconds));
                    var attemptTimeout = TimeSpan.FromSeconds(Math.Max(1, resilienceOptions.AttemptTimeoutSeconds));

                    if (attemptTimeout >= totalRequestTimeout)
                    {
                        attemptTimeout = totalRequestTimeout - TimeSpan.FromMilliseconds(100);
                    }

                    // ========== ATTEMPT TIMEOUT ==========
                    // Timeout for each individual attempt (before retry)
                    options.AttemptTimeout.Timeout = attemptTimeout;

                    // ========== TOTAL REQUEST TIMEOUT ==========
                    // Gateway-level timeout to prevent hanging connections
                    // This is the maximum time for the entire request including all retries
                    options.TotalRequestTimeout.Timeout = totalRequestTimeout;

                    // ========== RETRY POLICY ==========
                    // PRODUCTION: Minimal retries to avoid amplifying load
                    // Only retry on network failures and 5xx errors
                    options.Retry.MaxRetryAttempts = resilienceOptions.Retry.MaxRetryAttempts;
                    options.Retry.Delay = TimeSpan.FromMilliseconds(resilienceOptions.Retry.DelayMilliseconds);
                    options.Retry.BackoffType = resilienceOptions.Retry.UseExponentialBackoff 
                        ? DelayBackoffType.Exponential 
                        : DelayBackoffType.Constant;
                    options.Retry.UseJitter = resilienceOptions.Retry.UseJitter;

                    options.Retry.ShouldHandle = args =>
                    {
                        // Retry on network exceptions
                        if (args.Outcome.Exception is HttpRequestException)
                            return ValueTask.FromResult(true);

                        // Retry on timeout exceptions
                        if (args.Outcome.Exception is TimeoutRejectedException)
                            return ValueTask.FromResult(true);

                        var response = args.Outcome.Result;
                        if (response is null)
                            return ValueTask.FromResult(false);

                        // Retry on 408 Request Timeout and 5xx Server Errors
                        return ValueTask.FromResult(
                            response.StatusCode == HttpStatusCode.RequestTimeout ||
                            (int)response.StatusCode >= 500
                        );
                    };

                    // ========== CIRCUIT BREAKER ==========
                    // Prevents cascade failures by short-circuiting unhealthy downstream services
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(resilienceOptions.CircuitBreaker.SamplingDurationSeconds);
                    options.CircuitBreaker.MinimumThroughput = resilienceOptions.CircuitBreaker.MinimumThroughput;
                    options.CircuitBreaker.FailureRatio = resilienceOptions.CircuitBreaker.FailureRatio;
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(resilienceOptions.CircuitBreaker.BreakDurationSeconds);
                });

            return services;
        }

        /// <summary>
        /// Adds custom resilience pipeline with configurable parameters.
        /// Use this for specific routes that need different resilience settings.
        /// </summary>
        //public static IHttpClientBuilder AddCustomResilience(
        //    this IHttpClientBuilder builder,
        //    TimeSpan? timeout = null,
        //    int maxRetryAttempts = 1,
        //    double circuitBreakerFailureRatio = 0.5)
        //{
        //    return builder
        //        .AddHttpMessageHandler<CorrelationIdHandler>()
        //        .AddStandardResilienceHandler(options =>
        //        {
        //            options.TotalRequestTimeout.Timeout = timeout ?? TimeSpan.FromSeconds(4);
        //            options.Retry.MaxRetryAttempts = maxRetryAttempts;
        //            options.CircuitBreaker.FailureRatio = circuitBreakerFailureRatio;
        //        });
        //}
    }
}
