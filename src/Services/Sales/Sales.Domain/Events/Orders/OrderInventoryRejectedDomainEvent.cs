namespace Sales.Domain;

/// <summary>
/// Raised when Inventory rejects the reservation request (insufficient stock) and the
/// <see cref="Order"/> transitions to the InventoryRejected status.
/// </summary>
/// <param name="OrderId">Order that was rejected.</param>
/// <param name="Reason">Human-readable reason the reservation was rejected.</param>
public sealed record OrderInventoryRejectedDomainEvent(Guid OrderId, string Reason) : DomainEvent;
