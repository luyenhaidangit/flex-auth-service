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
            builder.ToTable("OutboxMessages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventType)
                .IsRequired()
                .HasMaxLength(200)
                .IsUnicode(false);

            builder.Property(x => x.Payload)
                .IsRequired()
                .HasColumnType("CLOB");

            builder.Property(x => x.OccurredOn)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(OutboxMessageStatus.Pending);

            builder.Property(x => x.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(2000)
                .IsUnicode(false);

            builder.Property(x => x.ProcessedOn)
                .IsRequired(false);

            // Index for querying pending messages
            builder.HasIndex(x => new { x.Status, x.OccurredOn })
                .HasDatabaseName("IX_OutboxMessages_Status_OccurredOn");
        }
    }
}
