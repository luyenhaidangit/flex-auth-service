using Flex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flex.Infrastructures.Persistence.Configurations
{
    internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
    {
        public void Configure(EntityTypeBuilder<InboxMessage> builder)
        {
            builder.ToTable("INBOX_MESSAGES");

            builder.HasKey(x => new { x.IdempotencyKey, x.Consumer });

            builder.Property(x => x.IdempotencyKey)
                .HasColumnName("IDEMPOTENCY_KEY")
                .IsRequired();

            builder.Property(x => x.Consumer)
                .HasColumnName("CONSUMER")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.ProcessedAt)
                .HasColumnName("PROCESSED_AT")
                .IsRequired();

            builder.Property(x => x.Payload)
                .HasColumnName("PAYLOAD")
                .HasColumnType("CLOB") // Oracle specific
                .IsRequired(false);

            builder.Property(x => x.OccurredAt)
                .HasColumnName("OCCURRED_AT")
                .IsRequired(false);
        }
    }
}
