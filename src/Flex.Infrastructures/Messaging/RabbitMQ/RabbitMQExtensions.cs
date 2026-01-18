using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.Messaging.RabbitMQ
{
    /// <summary>
    /// Extension methods for registering RabbitMQ services.
    /// </summary>
    public static class RabbitMQExtensions
    {
        /// <summary>
        /// Adds RabbitMQ publisher services to the service collection.
        /// Required declare RabbitMQ in configuration.
        /// </summary>
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddOptions<RabbitMQOptions>()
                .Bind(configuration.GetSection("RabbitMQ"))
                .Validate(o => !string.IsNullOrWhiteSpace(o.HostName), "RabbitMQ HostName is required")
                .Validate(o => !string.IsNullOrWhiteSpace(o.UserName), "RabbitMQ UserName is required")
                .Validate(o => !string.IsNullOrWhiteSpace(o.Password), "RabbitMQ Password is required")
                .ValidateOnStart();

            services.AddSingleton<IRabbitMQPublisher, RabbitMQPublisher>();

            return services;
        }
    }
}
