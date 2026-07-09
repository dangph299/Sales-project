namespace Sales.Domain;

/// <summary>
/// Raised when a new <see cref="Customer"/> aggregate is created.
/// </summary>
/// <param name="CustomerId">
/// The unique identifier of the customer that was created.
/// </param>
/// <param name="Name">
/// The customer's name at creation time.
/// </param>
/// <param name="Phone">
/// The customer's phone number at creation time.
/// </param>
public sealed record CustomerCreatedDomainEvent(Guid CustomerId, string Name, string Phone) : DomainEvent;
