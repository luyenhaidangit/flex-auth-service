using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.Events
{
    /// <summary>
    /// Extension methods for registering RabbitMQ services.
    /// </summary>
    public static class RabbitMQExtensions
    {
        /// <summary>
        /// Adds RabbitMQ publisher services to the service collection.
        /// </summary>
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind RabbitMQ options from configuration
            var options = configuration.GetSection("RabbitMQ").Get<RabbitMQOptions>() ?? new RabbitMQOptions();
            services.Configure<RabbitMQOptions>(configuration.GetSection("RabbitMQ"));

            // Register RabbitMQ publisher as singleton (connection is shared)
            services.AddSingleton<IRabbitMQPublisher>(sp =>
            {
                var configuredOptions = configuration.GetSection("RabbitMQ").Get<RabbitMQOptions>() ?? new RabbitMQOptions();
                return new RabbitMQPublisher(configuredOptions);
            });

            return services;
        }
    }
}
