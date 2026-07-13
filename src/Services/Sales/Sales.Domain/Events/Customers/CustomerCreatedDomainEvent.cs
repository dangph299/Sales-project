namespace Sales.Domain;

/// <summary>
/// Raised when a new <see cref="Customer"/> aggregate is created.
/// </summary>
/// <param name="CustomerId">Customer that was created.</param>
/// <param name="Name">Customer's name at creation time.</param>
/// <param name="Phone">Customer's phone number at creation time.</param>
public sealed record CustomerCreatedDomainEvent(Guid CustomerId, string Name, string Phone) : DomainEvent;
