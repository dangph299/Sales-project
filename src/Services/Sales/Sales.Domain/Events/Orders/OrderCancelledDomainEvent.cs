namespace Sales.Domain;

/// <summary>
/// Raised when an <see cref="Order"/> is cancelled, so Inventory can be asked to release any
/// reserved stock.
/// </summary>
/// <param name="OrderId">Order that was cancelled.</param>
public sealed record OrderUndoComfirmedDomainEvent(Guid OrderId) : DomainEvent;
