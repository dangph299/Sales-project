using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Groups Sales child entity changes under their owning aggregate root audit record.
/// </summary>
public sealed class SalesAuditAggregateResolver : IAuditAggregateResolver
{
    private readonly DefaultAuditAggregateResolver _defaultResolver = new();

    /// <inheritdoc/>
    public AuditAggregateIdentity Resolve(EntityEntry entityEntry)
    {
        if (entityEntry.Entity is OrderLine)
        {
            var orderId = GetGuid(entityEntry, nameof(OrderLine.OrderId));
            var productId = GetGuid(entityEntry, nameof(OrderLine.ProductId));
            return new AuditAggregateIdentity(
                nameof(Order),
                orderId.ToString(),
                orderId,
                $"Lines[ProductId={productId}]");
        }

        return _defaultResolver.Resolve(entityEntry);
    }

    private static Guid GetGuid(EntityEntry entityEntry, string propertyName)
    {
        var propertyEntry = entityEntry.Property(propertyName);
        var value = propertyEntry.CurrentValue ?? propertyEntry.OriginalValue;
        return value is Guid guid ? guid : Guid.Empty;
    }
}
