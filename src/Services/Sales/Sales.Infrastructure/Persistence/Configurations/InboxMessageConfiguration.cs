using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sales.Infrastructure;

/// <summary>
/// Persistence mapping for processed Sales messages (shared <see cref="InboxMessage"/> entity).
/// </summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<InboxMessage> entity)
    {
        // Table
        entity.ToTable("inbox_messages");

        // Primary Key
        entity.HasKey(x => x.EventId);

        // Properties
        // Sales always records the consumer id; keep the existing NOT NULL text column unchanged
        // even though the shared entity exposes Consumer as nullable for services that skip it.
        entity.Property(x => x.Consumer).IsRequired();
        entity.Property(x => x.LastExceptionType).HasMaxLength(512);
        entity.Property(x => x.LastError).HasMaxLength(2000);
        entity.Property(x => x.OriginalTopic).HasMaxLength(256);
        entity.Property(x => x.OriginalConsumerGroup).HasMaxLength(256);

        // Indexes
        entity.HasIndex(x => new { x.Status, x.DeadLetteredAt });
    }
}
