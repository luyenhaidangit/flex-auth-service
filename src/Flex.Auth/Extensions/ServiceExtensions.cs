using Flex.Domain.Entities;
using Flex.Auth.Events;
using Flex.Auth.Repositories;
using Flex.Auth.Repositories.Interfaces;
using Flex.Auth.Services;
using Flex.Auth.Services.Interfaces;
using Flex.Infrastructures.Authentication;
using Flex.Infrastructures.EntityFrameworkCore;
using Flex.Infrastructures.Http;
using Flex.Infrastructures.Messaging.Outbox;
using Flex.Infrastructures.Messaging.RabbitMQ;
using Flex.Infrastructures.Observability;
using Flex.Infrastructures.OpenApi;
using Flex.Infrastructures.Persistence;
using Flex.Infrastructures.RateLimiting;
using Flex.Infrastructures.Resilience;
using Flex.Infrastructures.Routing;
using Microsoft.AspNetCore.Identity;

namespace Flex.Auth.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddConfigurationSettings(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Controllers
            services.AddControllers();

            services.AddEndpointsApiExplorer();
            services.ConfigureSwagger();

            // Global Logging
            services.AddGlobalLogging(configuration, serviceName: "FlexAuthService");

            // Gateway
            services.AddGatewayAuthentication();
            services.AddGatewayAuthorization();
            services.AddTrustedForwardedHeaders(configuration);
            services.AddGatewayRateLimiting();

            // Resilience (Timeout, Retry, Circuit Breaker, Bulkhead)
            services.AddRequestContextAccessor();
            services.AddTransient<CorrelationIdHandler>();
            services.AddDownstreamResilience(configuration);

            // Database
            services.ConfigureServiceDbContext<IdentityDbContext>(configuration, useWallet: true);

            // Message queue
            services.AddRabbitMQ(configuration);
            services.AddOutbox<EventRoutingResolver>();

            // CORS
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Application Services
            services.AddApplicationServices();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();

            // Services
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();

            // Password Hasher
            services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

            return services;
        }
    }
}
