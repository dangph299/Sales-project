namespace Inventory.Domain;

/// <summary>
/// Command-side repository for stock reservations.
/// </summary>
public interface IReservationRepository
{
    /// <summary>
    /// Loads a reservation and its lines by Sales order id.
    /// </summary>
    Task<Reservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new reservation.
    /// </summary>
    void Add(Reservation reservation);
}
