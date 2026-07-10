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
/// <param name="UpdatedAt">
/// The UTC instant the product was last changed.
/// </param>
/// <param name="IsDelete">
/// Whether the product has been soft-deleted.
/// </param>
/// <param name="DeleteByUser">
/// The user that soft-deleted the product, or <see langword="null"/> if it is active.
/// </param>
/// <param name="DeletedAt">
/// The UTC instant the product was soft-deleted, or <see langword="null"/> if it is active.
/// </param>
public sealed record ProductDto(Guid Id, string Sku, string Name, decimal Price, bool IsActive, long Version,
    DateTimeOffset UpdatedAt, bool IsDelete, string? DeleteByUser, DateTimeOffset? DeletedAt);
