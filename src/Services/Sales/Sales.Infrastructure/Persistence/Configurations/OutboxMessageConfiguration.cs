using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sales.Infrastructure;

/// <summary>
/// Persistence mapping for outgoing reliable messages.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OutboxMessage> entity)
    {
        entity.ToTable("outbox_messages");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.ProcessedAt, x.OccurredAt });
        entity.HasIndex(x => new { x.DeadLetteredAt, x.NextAttemptAt, x.OccurredAt });
        entity.HasIndex(x => x.LockId);
        entity.Property(x => x.Payload).HasColumnType("jsonb");
    }
}
