namespace Inventory.Domain;

/// <summary>
/// The lifecycle status of a <see cref="Reservation"/>.
/// </summary>
public enum ReservationStatus
{
    /// <summary>The reservation currently holds stock.</summary>
    Active,

    /// <summary>The reservation's stock has been returned to available.</summary>
    Released
}
