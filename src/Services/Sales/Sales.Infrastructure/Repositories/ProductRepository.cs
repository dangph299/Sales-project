using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// EF Core implementation of <see cref="IProductRepository"/>, adding the SKU lookup on top of the
/// generic CRUD from <see cref="Repository{T}"/>.
/// </summary>
public sealed class ProductRepository(SalesDbContext db) : Repository<Product>(db), IProductRepository
{
    /// <inheritdoc/>
    public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        Db.Products.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Sku == sku, ct);
}
