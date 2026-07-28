using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Flex.Domain.Entities;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
    {
        public void Configure(EntityTypeBuilder<UserLogin> builder)
        {
            builder.ToTable("user_logins");

            builder.HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });

            builder.Property(ul => ul.LoginProvider)
                   .HasColumnName("login_provider");

            builder.Property(ul => ul.ProviderKey)
                   .HasColumnName("provider_key");

            builder.Property(ul => ul.ProviderDisplayName)
                   .HasColumnName("provider_display_name");

            builder.Property(ul => ul.UserId)
                   .HasColumnName("user_id");
        }
    }
}
