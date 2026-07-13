using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure;

/// <summary>
/// Persistence mapping for processed Inventory messages.
/// </summary>
public sealed class InboxRowConfiguration : IEntityTypeConfiguration<InboxRow>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<InboxRow> entity)
    {
        entity.ToTable("inbox_messages").HasKey(x => x.EventId);
    }
}
