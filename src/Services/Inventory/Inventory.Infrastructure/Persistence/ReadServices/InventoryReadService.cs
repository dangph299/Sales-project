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
    public async Task<ReservationSnapshot?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var reservation = await db.Reservations.Include(x => x.Lines)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        return reservation is null ? null : mapper.Map<ReservationSnapshot>(reservation);
    }
}
