using Flex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
    {
        public void Configure(EntityTypeBuilder<LoginHistory> builder)
        {
            builder.ToTable("login_histories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(x => x.UserName)
                .HasColumnName("user_name")
                .IsRequired()
                .HasMaxLength(256)
                .IsUnicode(false);

            builder.Property(x => x.LoginType)
                .HasColumnName("login_type")
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.Property(x => x.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.Property(x => x.Result)
                .HasColumnName("result")
                .IsRequired()
                .HasMaxLength(1)
                .IsUnicode(false);

            builder.Property(x => x.FailureReason)
                .HasColumnName("failure_reason")
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(x => x.OccurredOn)
                .HasColumnName("occurred_on")
                .IsRequired();

            // Index for querying by user
            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("ix_loginhistories_userid");

            // Index for querying by date
            builder.HasIndex(x => x.OccurredOn)
                .HasDatabaseName("ix_loginhistories_occurredon");

            // Composite index for common queries
            builder.HasIndex(x => new { x.UserId, x.OccurredOn })
                .HasDatabaseName("ix_loginhistories_userid_occurredon");
        }
    }
}
