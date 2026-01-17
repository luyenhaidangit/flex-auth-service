using Flex.Domain.Entities;
using Flex.Identity.Repositories;
using Flex.Identity.Repositories.Interfaces;
using Flex.Identity.Services;
using Flex.Identity.Services.Interfaces;
using Flex.Infrastructures.Authentication;
using Flex.Infrastructures.EntityFrameworkCore;
using Flex.Infrastructures.Events;
using Flex.Infrastructures.Messaging.Outbox;
using Flex.Infrastructures.Http;
using Flex.Infrastructures.Observability;
using Flex.Infrastructures.OpenApi;
using Flex.Infrastructures.Persistence;
using Flex.Infrastructures.RateLimiting;
using Flex.Infrastructures.Resilience;
using Flex.Infrastructures.Routing;
using Microsoft.AspNetCore.Identity;

namespace Flex.Identity.Extensions
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
            services.AddGlobalLogging(configuration, serviceName: "IdentityService");

            // Customize
            services.AddRoutingConventions();

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

            // RabbitMQ
            services.AddRabbitMQ(configuration);

            // Background Services
            services.AddHostedService<OutboxProcessorBackgroundService>();

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

            // Integration Events - Outbox
            services.AddScoped<IOutboxWriter, OutboxWriter>();
            services.AddScoped<IOutboxProcessor, OutboxProcessor>();

            return services;
        }
    }
}
