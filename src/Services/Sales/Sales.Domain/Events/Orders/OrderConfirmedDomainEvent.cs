namespace Sales.Domain;

/// <summary>
/// Raised when Inventory has successfully reserved stock and the <see cref="Order"/> transitions
/// to the Confirmed status.
/// </summary>
/// <param name="OrderId">Order that was confirmed.</param>
public sealed record OrderConfirmedDomainEvent(Guid OrderId) : DomainEvent;
