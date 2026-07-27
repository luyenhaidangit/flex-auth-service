using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.EntityFrameworkCore
{
    public static class EntityFrameworkCoreExtensions
    {
        public static IServiceCollection ConfigureServiceDbContext<TContext>(
            this IServiceCollection services,
            IConfiguration configuration,
            bool useWallet = false)
            where TContext : DbContext
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection string is missing or empty. Please define it in appsettings.json under ConnectionStrings.");
            }

            services.AddDbContext<TContext>(options =>
                options.UseNpgsql(connectionString));

            return services;
        }
    }
}
