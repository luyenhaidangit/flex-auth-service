using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.Messaging.Inbox
{
    public static class InboxExtensions
    {
        /// <summary>
        /// Adds inbox pattern services for message deduplication.
        /// </summary>
        public static IServiceCollection AddInbox(this IServiceCollection services)
        {
            services.AddScoped<IInboxStore, InboxStore>();
            return services;
        }
    }
}
