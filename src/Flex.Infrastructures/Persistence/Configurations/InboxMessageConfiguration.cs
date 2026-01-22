using Flex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
    {
        public void Configure(EntityTypeBuilder<InboxMessage> builder)
        {
            builder.ToTable("INBOX_MESSAGES");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("ID");

            builder.Property(x => x.MessageId)
                .HasColumnName("MESSAGE_ID")
                .IsRequired();

            // Unique constraint on MessageId for fast deduplication
            builder.HasIndex(x => x.MessageId)
                .IsUnique()
                .HasDatabaseName("UQ_INBOX_MESSAGES_MESSAGE_ID");

            builder.Property(x => x.Source)
                .HasColumnName("SOURCE")
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.EventType)
                .HasColumnName("EVENT_TYPE")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(x => x.HandlerName)
                .HasColumnName("HANDLER_NAME")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(x => x.BusinessKey)
                .HasColumnName("BUSINESS_KEY")
                .HasMaxLength(128);

            builder.Property(x => x.Payload)
                .HasColumnName("PAYLOAD")
                .HasColumnType("CLOB")
                .IsRequired();

            builder.Property(x => x.FirstSeenAt)
                .HasColumnName("FIRST_SEEN_AT")
                .IsRequired();

            builder.Property(x => x.ProcessedAt)
                .HasColumnName("PROCESSED_AT")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("STATUS")
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(x => x.ErrorMessage)
                .HasColumnName("ERROR_MESSAGE")
                .HasMaxLength(2000);

            // Index for business key lookups
            builder.HasIndex(x => x.BusinessKey)
                .HasDatabaseName("IX_INBOX_MESSAGES_BUSINESS_KEY");

            // Index for cleanup queries
            builder.HasIndex(x => x.ProcessedAt)
                .HasDatabaseName("IX_INBOX_MESSAGES_PROCESSED_AT");
        }
    }
}
