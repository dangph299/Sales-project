using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core mapping for <see cref="InboxRow"/>: keyed by <c>EventId</c> so a unique-constraint
/// violation on insert signals a duplicate Kafka message.
/// </summary>
public sealed class InboxRowConfiguration : IEntityTypeConfiguration<InboxRow>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<InboxRow> entity)
    {
        entity.ToTable("inbox_messages").HasKey(x => x.EventId);
    }
}
