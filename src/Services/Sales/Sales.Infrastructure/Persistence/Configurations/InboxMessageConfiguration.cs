using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sales.Infrastructure;

/// <summary>
/// Persistence mapping for processed Sales messages.
/// </summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<InboxMessage> entity)
    {
        entity.ToTable("inbox_messages").HasKey(x => x.EventId);
    }
}
