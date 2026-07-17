using Microsoft.Extensions.Caching.Distributed;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;

namespace Sales.Infrastructure;

/// <summary>
/// Product cache for catalog read models.
/// </summary>
public sealed class ProductCache : CacheService<ProductDto>, IProductCache
{
    /// <summary>
    /// Initializes the product cache with its backing distributed cache.
    /// </summary>
    /// <param name="cache">Distributed cache.</param>
    public ProductCache(IDistributedCache cache) : base(cache)
    {
    }

    /// <inheritdoc/>
    protected override string KeyPrefix => "catalog:product";

    /// <inheritdoc/>
    protected override Guid GetId(ProductDto value) => value.Id;
}
