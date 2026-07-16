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
    }
}
