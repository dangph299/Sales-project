namespace Sales.Domain;

/// <summary>
/// Raised when a new <see cref="Product"/> aggregate is created.
/// </summary>
/// <param name="ProductId">Product that was created.</param>
/// <param name="Sku">Product's SKU at creation time.</param>
/// <param name="Name">Product's name at creation time.</param>
/// <param name="Price">Product's price at creation time.</param>
public sealed record ProductCreatedDomainEvent(Guid ProductId, string Sku, string Name, decimal Price) : DomainEvent;
