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
            builder.ToTable("OUTBOX_MESSAGES");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("ID");

            builder.Property(x => x.EventType)
                .HasColumnName("EVENT_TYPE")
                .IsRequired()
                .HasMaxLength(200)
                .IsUnicode(false);

            builder.Property(x => x.Payload)
                .HasColumnName("PAYLOAD")
                .IsRequired()
                .HasColumnType("CLOB");

            builder.Property(x => x.Exchange)
                .HasColumnName("EXCHANGE")
                .IsRequired()
                .HasMaxLength(200)
                .IsUnicode(false);

            builder.Property(x => x.RoutingKey)
                .HasColumnName("ROUTING_KEY")
                .IsRequired()
                .HasMaxLength(200)
                .IsUnicode(false);

            builder.Property(x => x.OccurredOn)
                .HasColumnName("OCCURRED_ON")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("STATUS")
                .IsRequired()
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(OutboxMessageStatus.Pending);

            builder.Property(x => x.RetryCount)
                .HasColumnName("RETRY_COUNT")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.ErrorMessage)
                .HasColumnName("ERROR_MESSAGE")
                .HasMaxLength(2000)
                .IsUnicode(false);

            builder.Property(x => x.ProcessedOn)
                .HasColumnName("PROCESSED_ON")
                .IsRequired(false);

            // Index for querying pending messages
            builder.HasIndex(x => new { x.Status, x.OccurredOn })
                .HasDatabaseName("IX_OUTBOX_MESSAGES_STATUS_OCCURRED_ON");
        }
    }
}
