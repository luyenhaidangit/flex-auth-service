using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace Flex.Infrastructures.RateLimiting
{
    public static class RateLimitingExtensions
    {

        private const string GatewayConcurrencyLimiterPolicy = "gateway-concurrency-limiter";

        /// <summary>
        /// Configures global rate limiting using Token Bucket algorithm (100 tokens, 100 per second). Rate limits by user, client_id, or IP.
        /// Production-level configuration to handle normal traffic while preventing abuse.
        /// Note: Requires ForwardedHeaders configuration when behind a proxy to correctly identify client IPs.
        /// </summary>
        public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddConcurrencyLimiter(GatewayConcurrencyLimiterPolicy, limiter =>
                {
                    limiter.PermitLimit = 100000;
                    limiter.QueueLimit = 0;
                });

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var key = ResolveRateLimitKey(context);

                return RateLimitPartition.GetTokenBucketLimiter(
                    key,
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 100,
                        TokensPerPeriod = 100,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                        AutoReplenishment = true,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

                options.OnRejected = OnRateLimitRejectedAsync;
            });

            return services;
        }

        private static string ResolveRateLimitKey(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true &&
                !string.IsNullOrWhiteSpace(context.User.Identity.Name))
            {
                return $"user:{context.User.Identity.Name}";
            }

            if (context.Request.Headers.TryGetValue("client_id", out var clientId) &&
                !string.IsNullOrWhiteSpace(clientId))
            {
                return $"client:{clientId.ToString().Trim().ToLowerInvariant()}";
            }

            if (context.Connection?.RemoteIpAddress != null)
            {
                return $"ip:{context.Connection.RemoteIpAddress}";
            }

            return $"unknown:{context.Request.Path}";
        }

        private static async ValueTask OnRateLimitRejectedAsync(
            OnRejectedContext context,
            CancellationToken token)
        {
            if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    Math.Ceiling(retryAfter.TotalSeconds).ToString();
            }

            await context.HttpContext.Response.WriteAsync("Too many requests. Please retry later.", token);
        }
    }
}
