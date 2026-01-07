using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Flex.Infrastructures.Authentication
{
    public static class AuthenticationExtensions
    {
        private const string JwtOptionsKey = "JwtSettings";

        /// <summary>
        /// Configures JWT Bearer authentication with token validation from 'JwtSetting' configuration section.
        /// </summary>
        public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services)
        {
            services.AddOptions<JwtSettings>()
                .BindConfiguration(JwtOptionsKey)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

            services.AddOptions<JwtBearerOptions>()
                .Configure<IOptions<JwtSettings>>((jwtBearerOptions, jwtOptions) =>
                {
                    var settings = jwtOptions.Value;
                    jwtBearerOptions.MapInboundClaims = false;
                    jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = settings.Issuer,
                        ValidAudience = settings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey))
                    };
                });

            return services;
        }

        /// <summary>
        /// Configures authorization policies for the API Gateway.
        /// </summary>
        public static IServiceCollection AddGatewayAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Default policy: Require authenticated user
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                // Policy: Require authenticated user
                options.AddPolicy(AuthorizationPolicies.RequireAuthenticatedUser, policy =>
                    policy.RequireAuthenticatedUser());

                // Policy: Admin only
                options.AddPolicy(AuthorizationPolicies.RequireAdminRole, policy =>
                    policy.RequireRole("Admin"));

                // Policy: User or Admin
                options.AddPolicy(AuthorizationPolicies.RequireAdminOrUserRole, policy =>
                    policy.RequireRole("User", "Admin"));

                // Policy: Specific claims
                options.AddPolicy(AuthorizationPolicies.RequireEmailVerified, policy =>
                    policy.RequireClaim("email_verified", "true"));
            });

            return services;
        }
    }
}
