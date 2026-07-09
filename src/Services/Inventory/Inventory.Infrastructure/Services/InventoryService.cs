using BuildingBlocks.Contracts;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core-backed implementation of <see cref="IInventoryService"/>, called directly from
/// Inventory's Minimal API endpoints.
/// </summary>
public sealed class InventoryService(InventoryDbContext db) : IInventoryService
{
    /// <inheritdoc/>
    public async Task<InventorySnapshot?> GetAsync(Guid productId, CancellationToken ct = default)
    {
        var item = await db.Items.AsNoTracking().SingleOrDefaultAsync(x => x.ProductId == productId, ct);
        return item is null ? null : Map(item);
    }

    /// <inheritdoc/>
    public async Task<ReservationSnapshot?> GetReservationAsync(Guid orderId, CancellationToken ct = default)
    {
        var value = await db.Reservations.Include(x => x.Lines).AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == orderId, ct);
        return value is null ? null : new(value.OrderId, value.Status.ToString(), value.CreatedAt,
            value.Lines.Select(x => new ReservationLineSnapshot(x.ProductId, x.Sku, x.Quantity)).ToArray());
    }

    /// <inheritdoc/>
    public async Task<InventorySnapshot> AdjustAsync(Guid productId, string sku, int quantityDelta, string actor, CancellationToken ct = default)
    {
        var item = await db.Items.SingleOrDefaultAsync(x => x.ProductId == productId, ct);
        if (item is null)
        {
            item = InventoryItem.Create(productId, sku, quantityDelta);
            db.Items.Add(item);
        }
        else item.Adjust(quantityDelta);
        db.Enqueue(EventEnvelopeFactory.Create(productId, item.Version, new AuditChanged("InventoryItem", productId.ToString(), "Adjusted",
            [
                AuditChangeDetector.Change("QuantityDelta", null, quantityDelta, "Quantity Delta"),
                AuditChangeDetector.Change("Available", null, item.Available, "Available")
            ]), actor), KafkaTopics.InventoryAudit);
        await db.SaveChangesAsync(ct);
        return Map(item);
    }

    private static InventorySnapshot Map(InventoryItem item) => new(item.ProductId, item.Sku, item.Available, item.Reserved, item.Version);
}
