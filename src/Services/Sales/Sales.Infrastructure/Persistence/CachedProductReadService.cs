using Sales.Application;

namespace Sales.Infrastructure;

/// <summary>
/// Cache-aside decorator over <see cref="ProductReadService"/>: checks the product cache before
/// falling back to the database for <see cref="GetAsync"/>, and warms the cache on a miss.
/// <see cref="SearchAsync"/> is not cached and always delegates to the inner service.
/// </summary>
public sealed class CachedProductReadService(ProductReadService inner, IProductCache cache) : IProductReadService
{
    /// <inheritdoc/>
    public async Task<ProductDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync(id, cancellationToken);
        if (cached is not null) return cached;

        var product = await inner.GetAsync(id, cancellationToken);
        if (product is not null) await cache.SetAsync(product, cancellationToken);

        return product;
    }

    /// <inheritdoc/>
    public Task<PagedResult<ProductDto>> SearchAsync(
        string? name,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return inner.SearchAsync(name, page, pageSize, cancellationToken);
    }
}
