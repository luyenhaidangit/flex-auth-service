using Flex.Domain.Entities;
using Flex.Infrastructures.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("roles");

            // Keys
            builder.HasKey(r => r.Id);

            // Columns
            builder.Property(r => r.Id)
                    .HasColumnName("id");

            builder.Property(r => r.Name)
                   .HasColumnName("name").HasMaxLength(256);

            builder.Property(r => r.NormalizedName)
                   .HasColumnName("normalized_name")
                   .HasMaxLength(256)
                   .IsRequired();

            builder.Property(r => r.Code)
                   .HasColumnName("code")
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(r => r.Description)
                   .HasColumnName("description");

            builder.Property(r => r.Status)
                   .HasColumnName("status")
                   .HasMaxLength(50);

            builder.Property(r => r.IsActive)
                   .HasColumnName("is_active")
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.Property(r => r.ConcurrencyStamp)
                   .HasColumnName("concurrency_stamp")
                   .IsConcurrencyToken();

            // Unique indexes theo chuẩn Identity
            builder.HasIndex(r => r.NormalizedName)
                   .HasDatabaseName("ux_roles_normalized_name")
                   .IsUnique();

            builder.HasIndex(r => r.Code)
                   .HasDatabaseName("ux_roles_code")
                   .IsUnique();
        }
    }
}
