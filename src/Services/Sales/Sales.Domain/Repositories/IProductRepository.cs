namespace Sales.Domain;

/// <summary>
/// Command-side persistence contract for <see cref="Product"/>, adding catalog-specific lookups.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Loads a single product by its SKU.
    /// </summary>
    /// <param name="sku">SKU.</param>
    /// <returns>Product with the given SKU, or <see langword="null"/> if none exists.</returns>
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<Product?> GetByProductCodeAsync(string productCode, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Product?>(null);
    }

    Task<Product?> GetWithVariantsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(id, cancellationToken);
    }

    Task<IReadOnlyList<Product>> GetWithVariantsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return GetByIdsAsync(ids, cancellationToken);
    }

    Task<ProductVariant?> GetVariantAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ProductVariant?>(null);
    }

    Task<IReadOnlyList<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ProductVariant>>([]);
    }

    Task<Color?> GetColorAsync(Guid colorId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Color?>(null);
    }

    Task<Size?> GetSizeAsync(Guid sizeId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Size?>(null);
    }
}
