using Inventory.Application;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core read service for Inventory queries.
/// </summary>
public sealed class InventoryReadService(InventoryDbContext db) : IInventoryReadService
{
    /// <inheritdoc/>
    public async Task<InventorySnapshot?> GetAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var item = await db.Items.AsNoTracking().SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        return item?.ToSnapshot();
    }

    /// <inheritdoc/>
    public async Task<ReservationSnapshot?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var value = await db.Reservations.Include(x => x.Lines)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        return value is null
            ? null
            : new ReservationSnapshot(
                value.OrderId,
                value.Status.ToString(),
                value.CreatedAt,
                value.Lines.Select(x => new ReservationLineSnapshot(x.ProductId, x.Sku, x.Quantity)).ToArray());
    }
}
