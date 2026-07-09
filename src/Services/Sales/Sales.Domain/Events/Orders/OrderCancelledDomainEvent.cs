namespace Sales.Domain;

/// <summary>
/// Raised when an <see cref="Order"/> is cancelled, so Inventory can be asked to release any
/// reserved stock.
/// </summary>
/// <param name="OrderId">
/// The unique identifier of the order that was cancelled.
/// </param>
public sealed record OrderCancelledDomainEvent(Guid OrderId) : DomainEvent;
