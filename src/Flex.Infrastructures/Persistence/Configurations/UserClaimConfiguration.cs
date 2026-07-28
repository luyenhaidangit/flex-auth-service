using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Flex.Domain.Entities;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
    {
        public void Configure(EntityTypeBuilder<UserClaim> builder)
        {
            builder.ToTable("user_claims");
            builder.HasKey(uc => uc.Id);
            builder.Property(uc => uc.Id).HasColumnName("id");
            builder.Property(uc => uc.UserId).HasColumnName("user_id");
            builder.Property(uc => uc.ClaimType).HasColumnName("claim_type").HasMaxLength(256);
            builder.Property(uc => uc.ClaimValue).HasColumnName("claim_value").HasMaxLength(256);
        }
    }
}
