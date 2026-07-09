namespace Sales.Domain;

/// <summary>
/// Raised when a new <see cref="Order"/> aggregate is created.
/// </summary>
/// <param name="OrderId">
/// The unique identifier of the order that was created.
/// </param>
/// <param name="CustomerId">
/// The unique identifier of the customer the order was placed for.
/// </param>
public sealed record OrderCreatedDomainEvent(Guid OrderId, Guid CustomerId) : DomainEvent;
