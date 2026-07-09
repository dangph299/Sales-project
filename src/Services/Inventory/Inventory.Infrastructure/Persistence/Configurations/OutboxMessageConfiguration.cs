using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core mapping for <see cref="OutboxMessage"/>: table/index configuration supporting the
/// publisher's polling queries (unpublished rows ready to retry, dead-lettered rows, locked rows).
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OutboxMessage> entity)
    {
        entity.ToTable("outbox_messages").HasKey(x => x.Id);
        entity.Property(x => x.Payload).HasColumnType("jsonb");
        entity.HasIndex(x => new { x.ProcessedAt, x.OccurredAt });
        entity.HasIndex(x => new { x.DeadLetteredAt, x.NextAttemptAt, x.OccurredAt });
        entity.HasIndex(x => x.LockId);
    }
}
