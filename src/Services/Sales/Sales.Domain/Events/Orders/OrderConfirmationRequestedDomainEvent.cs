namespace Sales.Domain;

/// <summary>
/// Raised when an <see cref="Order"/> requests confirmation, so Inventory can be asked to reserve
/// stock for each line.
/// </summary>
/// <param name="OrderId">
/// The unique identifier of the order requesting confirmation.
/// </param>
/// <param name="Lines">
/// The product/quantity pairs that Inventory must reserve stock for.
/// </param>
public sealed record OrderConfirmationRequestedDomainEvent(Guid OrderId, IReadOnlyCollection<OrderLineReservation> Lines) : DomainEvent;
