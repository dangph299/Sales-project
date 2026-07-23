using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.InventoryItems.Interfaces;
using Inventory.Application.Features.Reservations.DTOs;
using Inventory.Application.Features.Reservations.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core read service for Inventory queries.
/// </summary>
public sealed class InventoryReadService(InventoryDbContext db, IMapper mapper)
    : IInventoryItemReadService, IReservationReadService
{
    /// <inheritdoc/>
    public async Task<InventorySnapshot?> GetAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var item = await db.Items.AsNoTracking().SingleOrDefaultAsync(x => x.ProductVariantId == productId, cancellationToken);
        return item is null ? null : mapper.Map<InventorySnapshot>(item);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<InventorySnapshot>> GetByProductVariantIdsAsync(
        IReadOnlyCollection<Guid> productVariantIds,
        CancellationToken cancellationToken = default)
    {
        var ids = productVariantIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        var items = await db.Items.AsNoTracking()
            .Where(x => ids.Contains(x.ProductVariantId))
            .Select(x => new InventorySnapshot(x.ProductVariantId, x.Sku, x.Available, x.Reserved, x.Version))
            .ToDictionaryAsync(x => x.ProductId, cancellationToken);

        return ids
            .Select(id => items.GetValueOrDefault(id) ?? new InventorySnapshot(id, string.Empty, 0, 0, 0))
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<ReservationSnapshot?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var reservation = await db.Reservations.Include(x => x.Lines)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        return reservation is null ? null : mapper.Map<ReservationSnapshot>(reservation);
    }

    /// <inheritdoc/>
    public async Task<InventorySummary> GetSummaryAsync(
        InventorySummaryFilter filter, CancellationToken cancellationToken = default)
    {
        var threshold = filter.LowStockThreshold;
        var agg = await db.Items.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalItems = g.Count(),
                TotalQuantity = (long)g.Sum(x => x.Available),
                InStock = g.Count(x => x.Available > threshold),
                LowStock = g.Count(x => x.Available > 0 && x.Available <= threshold),
                OutOfStock = g.Count(x => x.Available == 0)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new InventorySummary(
            agg?.TotalItems ?? 0, agg?.TotalQuantity ?? 0,
            agg?.InStock ?? 0, agg?.LowStock ?? 0, agg?.OutOfStock ?? 0, threshold);
    }
}
