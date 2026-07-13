using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Product persistence adapter with catalog-specific lookups.
/// </summary>
public sealed class ProductRepository(SalesDbContext db) : Repository<Product>(db), IProductRepository
{
    /// <inheritdoc/>
    public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        Db.Products.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Sku == sku, ct);
}
