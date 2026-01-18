using Flex.Infrastructures.Messaging.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        /// Application layer can override IEventRoutingResolver to customize routing strategy.
        /// </summary>
        public static IServiceCollection AddOutbox(this IServiceCollection services)
        {
            // Register event routing resolver. Application layer can override this.
            services.AddSingleton<IEventRoutingResolver, DefaultEventRoutingResolver>();

            // Register outbox services
            services.AddScoped<IOutboxWriter, OutboxWriter>();
            services.AddScoped<IOutboxProcessor, OutboxProcessor>();

            return services;
        }
    }
}
