namespace Sales.Domain;

/// <summary>
/// Raised when an existing <see cref="Customer"/> aggregate's name or phone number is changed.
/// </summary>
/// <param name="CustomerId">Customer that was updated.</param>
/// <param name="OldName">Customer's name before the update.</param>
/// <param name="OldPhone">Customer's phone number before the update.</param>
/// <param name="NewName">Customer's name after the update.</param>
/// <param name="NewPhone">Customer's phone number after the update.</param>
public sealed record CustomerUpdatedDomainEvent(
    Guid CustomerId,
    string OldName,
    string OldPhone,
    string NewName,
    string NewPhone) : DomainEvent;
