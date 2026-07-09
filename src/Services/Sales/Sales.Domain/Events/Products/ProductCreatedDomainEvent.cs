namespace Sales.Domain;

/// <summary>
/// Raised when a new <see cref="Product"/> aggregate is created.
/// </summary>
/// <param name="ProductId">
/// The unique identifier of the product that was created.
/// </param>
/// <param name="Sku">
/// The product's SKU at creation time.
/// </param>
/// <param name="Name">
/// The product's name at creation time.
/// </param>
/// <param name="Price">
/// The product's price at creation time.
/// </param>
public sealed record ProductCreatedDomainEvent(Guid ProductId, string Sku, string Name, decimal Price) : DomainEvent;
