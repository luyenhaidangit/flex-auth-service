using Flex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.Property(u => u.Id)
                   .HasColumnName("id");

            builder.Property(u => u.UserName)
                   .HasColumnName("user_name")
                   .HasMaxLength(256);

            builder.Property(u => u.NormalizedUserName)
                   .HasColumnName("normalized_user_name")
                   .HasMaxLength(256)
                   .IsRequired();

            builder.Property(u => u.Email)
                   .HasColumnName("email")
                   .HasMaxLength(256);

            builder.Property(u => u.NormalizedEmail)
                   .HasColumnName("normalized_email")
                   .HasMaxLength(256);

            builder.Property(u => u.EmailConfirmed)
                   .HasColumnName("email_confirmed")
                   .HasDefaultValue(false);

            builder.Property(u => u.PasswordHash).HasColumnName("password_hash");
            builder.Property(u => u.SecurityStamp).HasColumnName("security_stamp");
            builder.Property(u => u.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();

            builder.Property(u => u.PhoneNumber)
                   .HasColumnName("phone_number")
                   .HasMaxLength(50);

            builder.Property(u => u.PhoneNumberConfirmed)
                   .HasColumnName("phone_number_confirmed")
                   .HasDefaultValue(false);

            builder.Property(u => u.TwoFactorEnabled)
                   .HasColumnName("two_factor_enabled")
                   .HasDefaultValue(false);

            builder.Property(u => u.LockoutEnd).HasColumnName("lockout_end");
            builder.Property(u => u.LockoutEnabled)
                   .HasColumnName("lockout_enabled")
                   .HasDefaultValue(false);

            builder.Property(u => u.AccessFailedCount).HasColumnName("access_failed_count");

            builder.Property(u => u.FullName)
                   .HasColumnName("full_name")
                   .HasMaxLength(250);

            // Indexes (the same convention as Role)
            builder.HasIndex(u => u.NormalizedUserName)
                   .HasDatabaseName("ux_users_normalized_user_name")
                   .IsUnique();

            builder.HasIndex(u => u.NormalizedEmail)
                   .HasDatabaseName("ix_users_normalized_email");
        }
    }
}
