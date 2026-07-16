using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure;

/// <summary>
/// Persistence mapping for processed Inventory messages (shared <see cref="InboxMessage"/> entity).
/// </summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<InboxMessage> entity)
    {
        entity.ToTable("inbox_messages");
        entity.HasKey(x => x.EventId);
        entity.Property(x => x.Consumer).HasMaxLength(64);
        entity.Property(x => x.LastExceptionType).HasMaxLength(512);
        entity.Property(x => x.LastError).HasMaxLength(2000);
        entity.Property(x => x.OriginalTopic).HasMaxLength(256);
        entity.Property(x => x.OriginalConsumerGroup).HasMaxLength(256);
        entity.HasIndex(x => new { x.Status, x.DeadLetteredAt });
    }
}
