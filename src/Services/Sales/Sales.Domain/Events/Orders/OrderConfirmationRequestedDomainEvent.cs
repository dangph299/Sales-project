namespace Sales.Domain;

/// <summary>
/// Raised when an <see cref="Order"/> requests confirmation, so Inventory can be asked to reserve
/// stock for each line.
/// </summary>
/// <param name="OrderId">Order requesting confirmation.</param>
/// <param name="Lines">Product/quantity pairs that Inventory must reserve stock for.</param>
public sealed record OrderConfirmationRequestedDomainEvent(Guid OrderId, IReadOnlyCollection<OrderLineReservation> Lines) : DomainEvent;
