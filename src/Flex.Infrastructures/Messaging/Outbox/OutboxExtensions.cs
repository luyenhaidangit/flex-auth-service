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
        /// This registers the IEventRoutingResolver, IOutboxWriter, and IOutboxProcessor.
        /// Application layer must provide the routing resolver implementation.
        /// </summary>
        public static IServiceCollection AddOutbox<TResolver>(this IServiceCollection services)
            where TResolver : class, IEventRoutingResolver
        {
            // Register event routing resolver (Application layer implementation)
            services.AddSingleton<IEventRoutingResolver, TResolver>();

            // Register outbox services
            services.AddScoped<IOutboxWriter, OutboxWriter>();
            services.AddScoped<IOutboxProcessor, OutboxProcessor>();

            return services;
        }
    }
}
