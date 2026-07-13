namespace Sales.Domain;

/// <summary>
/// Raised when a new <see cref="Order"/> aggregate is created.
/// </summary>
/// <param name="OrderId">Order that was created.</param>
/// <param name="CustomerId">Customer the order was placed for.</param>
public sealed record OrderCreatedDomainEvent(Guid OrderId, Guid CustomerId) : DomainEvent;
