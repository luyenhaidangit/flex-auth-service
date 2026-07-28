using Flex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
    {
        public void Configure(EntityTypeBuilder<InboxMessage> builder)
        {
            builder.ToTable("inbox_messages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.MessageId)
                .HasColumnName("message_id")
                .IsRequired();

            // Unique constraint on (MessageId, HandlerName) for deduplication per handler
            builder.HasIndex(x => new { x.MessageId, x.HandlerName })
                .IsUnique()
                .HasDatabaseName("uq_inbox_dedup");

            builder.Property(x => x.Source)
                .HasColumnName("source")
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(x => x.HandlerName)
                .HasColumnName("handler_name")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(x => x.BusinessKey)
                .HasColumnName("business_key")
                .HasMaxLength(128);

            builder.Property(x => x.Payload)
                .HasColumnName("payload")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(x => x.FirstSeenAt)
                .HasColumnName("first_seen_at")
                .IsRequired();

            builder.Property(x => x.ProcessedAt)
                .HasColumnName("processed_at")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(x => x.RetryCount)
                .HasColumnName("retry_count")
                .IsRequired();

            builder.Property(x => x.ErrorMessage)
                .HasColumnName("error_message")
                .HasMaxLength(2000);

            // Index for business key lookups
            builder.HasIndex(x => x.BusinessKey)
                .HasDatabaseName("ix_inbox_messages_business_key");

            // Index for cleanup queries
            builder.HasIndex(x => x.ProcessedAt)
                .HasDatabaseName("ix_inbox_messages_processed_at");
        }
    }
}
