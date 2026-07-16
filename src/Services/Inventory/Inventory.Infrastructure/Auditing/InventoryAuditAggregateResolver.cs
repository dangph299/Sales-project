using BuildingBlocks.Infrastructure;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Inventory.Infrastructure;

/// <summary>
/// Groups Inventory child entity changes under their owning aggregate root audit record.
/// </summary>
public sealed class InventoryAuditAggregateResolver : IAuditAggregateResolver
{
    private readonly DefaultAuditAggregateResolver _defaultResolver = new();

    /// <inheritdoc/>
    public AuditAggregateIdentity Resolve(EntityEntry entityEntry)
    {
        if (entityEntry.Entity is ReservationLine)
        {
            var reservationId = GetGuid(entityEntry, nameof(ReservationLine.ReservationId));
            var productId = GetGuid(entityEntry, nameof(ReservationLine.ProductId));
            return new AuditAggregateIdentity(
                nameof(Reservation),
                reservationId.ToString(),
                reservationId,
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
