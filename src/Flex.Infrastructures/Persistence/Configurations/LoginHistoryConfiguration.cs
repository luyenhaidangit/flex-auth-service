using Flex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
    {
        public void Configure(EntityTypeBuilder<LoginHistory> builder)
        {
            builder.ToTable("LoginHistories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.UserName)
                .IsRequired()
                .HasMaxLength(256)
                .IsUnicode(false);

            builder.Property(x => x.LoginType)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.Property(x => x.IpAddress)
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.Property(x => x.Result)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.Property(x => x.FailureReason)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(x => x.OccurredOn)
                .IsRequired();

            // Index for querying by user
            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_LoginHistories_UserId");

            // Index for querying by date
            builder.HasIndex(x => x.OccurredOn)
                .HasDatabaseName("IX_LoginHistories_OccurredOn");

            // Composite index for common queries
            builder.HasIndex(x => new { x.UserId, x.OccurredOn })
                .HasDatabaseName("IX_LoginHistories_UserId_OccurredOn");
        }
    }
}
