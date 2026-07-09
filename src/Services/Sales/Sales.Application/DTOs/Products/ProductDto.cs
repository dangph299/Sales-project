namespace Sales.Application;

/// <summary>
/// Read model for a product, returned by queries, API responses, and the product cache.
/// </summary>
/// <param name="Id">
/// The product's unique identifier.
/// </param>
/// <param name="Sku">
/// The product's normalized SKU.
/// </param>
/// <param name="Name">
/// The product's name.
/// </param>
/// <param name="Price">
/// The product's unit price in VND.
/// </param>
/// <param name="IsActive">
/// Whether the product can currently be ordered.
/// </param>
/// <param name="Version">
/// The product's current optimistic concurrency version.
/// </param>
public sealed record ProductDto(Guid Id, string Sku, string Name, decimal Price, bool IsActive, long Version);
