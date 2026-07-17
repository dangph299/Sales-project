using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core repository for reservation aggregates.
/// </summary>
public sealed class ReservationRepository(InventoryDbContext db) : IReservationRepository
{
    /// <inheritdoc/>
    public Task<Reservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return db.Reservations
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
    }

    /// <inheritdoc/>
    public void Add(Reservation reservation)
    {
        db.Reservations.Add(reservation);
    }
}
