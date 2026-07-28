using Flex.Domain.Constants;
using Flex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flex.Infrastructures.Persistence.Configurations
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("outbox_messages");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.EventType)
                .HasColumnName("event_type")
                .IsRequired()
                .HasMaxLength(200)
                .IsUnicode(false);

            builder.Property(x => x.Payload)
                .HasColumnName("payload")
                .IsRequired()
                .HasColumnType("text");

            builder.Property(x => x.Exchange)
                .HasColumnName("exchange")
                .IsRequired()
                .HasMaxLength(200)
                .IsUnicode(false);

            builder.Property(x => x.RoutingKey)
                .HasColumnName("routing_key")
                .IsRequired()
                .HasMaxLength(200)
                .IsUnicode(false);

            builder.Property(x => x.OccurredOn)
                .HasColumnName("occurred_on")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(OutboxMessageStatus.Pending);

            builder.Property(x => x.RetryCount)
                .HasColumnName("retry_count")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.ErrorMessage)
                .HasColumnName("error_message")
                .HasMaxLength(2000)
                .IsUnicode(false);

            builder.Property(x => x.ProcessedOn)
                .HasColumnName("processed_on")
                .IsRequired(false);

            // Index for querying pending messages
            builder.HasIndex(x => new { x.Status, x.OccurredOn })
                .HasDatabaseName("ix_outbox_messages_status_occurred_on");
        }
    }
}
