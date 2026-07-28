using Flex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("permissions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnName("code");

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            builder.Property(x => x.IsAssignable)
                .IsRequired()
                .HasDefaultValue(true)
                .HasColumnName("is_assignable");

            builder.Property(x => x.SortOrder)
                .IsRequired()
                .HasColumnName("sort_order");

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasColumnName("is_active");

            builder.Property(x => x.IsCrudRule)
                .IsRequired()
                .HasDefaultValue(0)
                .HasColumnName("is_crud_rule");

            builder.Property(x => x.ParentPermissionId)
                .HasColumnName("parent_permission_id");

            builder.HasIndex(x => x.Code).IsUnique();

            builder.HasOne(x => x.ParentPermission)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentPermissionId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
