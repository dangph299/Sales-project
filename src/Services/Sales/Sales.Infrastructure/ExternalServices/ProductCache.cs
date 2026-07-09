using Microsoft.Extensions.Caching.Distributed;
using Sales.Application;

namespace Sales.Infrastructure;

/// <summary>
/// Cache-aside implementation of <see cref="IProductCache"/>, storing entries under the
/// <c>catalog:product</c> key prefix.
/// </summary>
public sealed class ProductCache : CacheService<ProductDto>, IProductCache
{
    /// <summary>
    /// Initializes the product cache with its backing distributed cache.
    /// </summary>
    /// <param name="cache">
    /// The distributed cache to read/write through.
    /// </param>
    public ProductCache(IDistributedCache cache) : base(cache)
    {
    }

    /// <inheritdoc/>
    protected override string KeyPrefix => "catalog:product";

    /// <inheritdoc/>
    protected override Guid GetId(ProductDto value) => value.Id;
}
