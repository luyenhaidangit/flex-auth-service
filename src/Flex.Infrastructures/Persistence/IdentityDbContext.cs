using Flex.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flex.Infrastructures.Persistence
{
    public class IdentityDbContext : DbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.DefaultTypeMapping<string>(
                b => b.IsUnicode(false));
        }

        #region Tables
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RoleClaim> RoleClaims { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        #endregion
    }
}
