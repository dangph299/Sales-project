using Microsoft.EntityFrameworkCore;
using Sales.Application;

namespace Sales.Infrastructure;

/// <summary>
/// Read-side product lookup service for query handlers.
/// Not cached itself — see <see cref="CachedProductReadService"/> for the cache-aside decorator
/// registered as <see cref="IProductReadService"/>.
/// </summary>
public sealed class ProductReadService(SalesDbContext db) : IProductReadService
{
    /// <inheritdoc/>
    public async Task<ProductDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return product?.ToDto();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<ProductDto>> SearchAsync(string? name, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var query = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => EF.Functions.ILike(x.Name, $"%{name.Trim()}%"));
        var total = await query.LongCountAsync(ct);
        var products = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(products.Select(x => x.ToDto()).ToArray(), page, pageSize, total);
    }
}
