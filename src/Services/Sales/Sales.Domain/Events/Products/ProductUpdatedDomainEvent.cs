namespace Sales.Domain;

/// <summary>
/// Raised when an existing <see cref="Product"/> aggregate's name, price, or active flag is changed.
/// </summary>
/// <param name="ProductId">
/// The unique identifier of the product that was updated.
/// </param>
/// <param name="OldName">
/// The product's name before the update.
/// </param>
/// <param name="OldPrice">
/// The product's price before the update.
/// </param>
/// <param name="OldIsActive">
/// Whether the product was active before the update.
/// </param>
/// <param name="NewName">
/// The product's name after the update.
/// </param>
/// <param name="NewPrice">
/// The product's price after the update.
/// </param>
/// <param name="NewIsActive">
/// Whether the product is active after the update.
/// </param>
public sealed record ProductUpdatedDomainEvent(
    Guid ProductId,
    string OldName,
    decimal OldPrice,
    bool OldIsActive,
    string NewName,
    decimal NewPrice,
    bool NewIsActive) : DomainEvent;
