using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Flex.Domain.Entities;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
    {
        public void Configure(EntityTypeBuilder<RoleClaim> builder)
        {
            builder.ToTable("role_claims");

            builder.HasKey(rc => rc.Id);
            builder.Property(rc => rc.Id).HasColumnName("id");

            builder.Property(rc => rc.RoleId).HasColumnName("role_id");
            builder.Property(rc => rc.ClaimType).HasColumnName("claim_type").HasMaxLength(100).IsRequired();
            builder.Property(rc => rc.ClaimValue).HasColumnName("claim_value").HasMaxLength(100);
        }
    }
}
