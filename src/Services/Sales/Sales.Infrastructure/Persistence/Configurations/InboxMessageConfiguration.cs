using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sales.Infrastructure;

/// <summary>
/// EF Core mapping for <see cref="InboxMessage"/>: keyed by <c>EventId</c> so a unique-constraint
/// violation on insert signals a duplicate Kafka message.
/// </summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<InboxMessage> entity)
    {
        entity.ToTable("inbox_messages").HasKey(x => x.EventId);
    }
}
