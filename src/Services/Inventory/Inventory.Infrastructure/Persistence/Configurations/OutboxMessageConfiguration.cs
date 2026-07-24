using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure;

/// <summary>
/// Persistence mapping for outgoing reliable messages.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OutboxMessage> entity)
    {
        // Table
        entity.ToTable("outbox_messages");

        // Primary Key
        entity.HasKey(x => x.Id);

        // Properties
        entity.Property(x => x.Payload).HasColumnType("jsonb");

        // Indexes
        entity.HasIndex(x => new { x.ProcessedAt, x.OccurredAt });
        entity.HasIndex(x => new { x.DeadLetteredAt, x.NextAttemptAt, x.OccurredAt });
        entity.HasIndex(x => x.LockId);
    }
}
