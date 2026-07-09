namespace Sales.Domain;

/// <summary>
/// Raised when an existing <see cref="Customer"/> aggregate's name or phone number is changed.
/// </summary>
/// <param name="CustomerId">
/// The unique identifier of the customer that was updated.
/// </param>
/// <param name="OldName">
/// The customer's name before the update.
/// </param>
/// <param name="OldPhone">
/// The customer's phone number before the update.
/// </param>
/// <param name="NewName">
/// The customer's name after the update.
/// </param>
/// <param name="NewPhone">
/// The customer's phone number after the update.
/// </param>
public sealed record CustomerUpdatedDomainEvent(
    Guid CustomerId,
    string OldName,
    string OldPhone,
    string NewName,
    string NewPhone) : DomainEvent;
