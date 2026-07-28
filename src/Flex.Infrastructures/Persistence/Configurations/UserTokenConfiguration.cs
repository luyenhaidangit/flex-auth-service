using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Flex.Domain.Entities;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
    {
        public void Configure(EntityTypeBuilder<UserToken> builder)
        {
            builder.ToTable("user_tokens");

            builder.HasKey(ut => new { ut.UserId, ut.LoginProvider, ut.Name });

            builder.Property(ut => ut.UserId)
                   .HasColumnName("user_id");

            builder.Property(ut => ut.LoginProvider)
                   .HasColumnName("login_provider");

            builder.Property(ut => ut.Name)
                   .HasColumnName("name");

            builder.Property(ut => ut.Value)
                   .HasColumnName("value");
        }
    }
}
