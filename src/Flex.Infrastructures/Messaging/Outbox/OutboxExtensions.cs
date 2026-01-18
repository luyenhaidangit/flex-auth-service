using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.Messaging.Outbox
{
    /// <summary>
    /// Extension methods for registering Outbox services.
    /// </summary>
    public static class OutboxExtensions
    {
        /// <summary>
        /// Adds Outbox services to the service collection.
        /// This registers the IOutboxWriter for writing integration events to the outbox table
        /// and IOutboxProcessor for processing pending messages.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOutbox(this IServiceCollection services)
        {
            services.AddScoped<IOutboxWriter, OutboxWriter>();
            services.AddScoped<IOutboxProcessor, OutboxProcessor>();

            return services;
        }
    }
}
