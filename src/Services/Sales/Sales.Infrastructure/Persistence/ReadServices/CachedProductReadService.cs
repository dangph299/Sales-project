using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;
namespace Sales.Infrastructure;

/// <summary>
/// Cache-aside decorator over <see cref="ProductReadService"/>: checks the product cache before
/// falling back to the database for <see cref="GetAsync"/>, and warms the cache on a miss.
/// <see cref="SearchAsync"/> and variant lookup are not cached and always delegate to the inner
/// service.
/// </summary>
public sealed class CachedProductReadService(IProductReadService inner, IProductCache cache) : IProductReadService
{
    /// <inheritdoc/>
    public async Task<ProductDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync(id, cancellationToken);
        if (cached is not null)
        {
            if (IsActive(cached))
            {
                return cached;
            }

            await cache.RemoveAsync(id, cancellationToken);
        }

        var product = await inner.GetAsync(id, cancellationToken);
        if (product is not null)
        {
            await cache.SetAsync(product, cancellationToken);
        }

        return product;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Deliberately uncached: the cache only holds published products, and this read exists to
    /// return a product a command just wrote, whatever status it landed in.
    /// </remarks>
    public Task<ProductDto?> GetForWriteResultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return inner.GetForWriteResultAsync(id, cancellationToken);
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

    /// <inheritdoc/>
    public Task<PagedResult<ProductDto>> SearchAsync(
        string? productCode,
        string? name,
        Guid? categoryId,
        string? sku,
        Guid? colorId,
        Guid? sizeId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return inner.SearchAsync(productCode, name, categoryId, sku, colorId, sizeId, status, page, pageSize, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<PagedResult<ProductVariantLookupDto>> SearchVariantsAsync(
        string? productCode,
        string? productName,
        string? sku,
        string? variantStatus,
        string? sortBy,
        string? sortDirection,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return inner.SearchVariantsAsync(
            productCode,
            productName,
            sku,
            variantStatus,
            sortBy,
            sortDirection,
            page,
            pageSize,
            cancellationToken);
    }

    private static bool IsActive(ProductDto product)
    {
        return product.IsActive && !product.IsDelete;
    }
}
