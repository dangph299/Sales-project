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
        Db.ProductVariants.IgnoreQueryFilters()
            .Where(x => x.Sku == sku)
            .Select(x => x.ProductId)
            .Join(Db.Products.IgnoreQueryFilters(), productId => productId, product => product.Id, (_, product) => product)
            .SingleOrDefaultAsync(ct);

    public Task<Product?> GetByProductCodeAsync(string productCode, CancellationToken cancellationToken = default)
    {
        return Db.Products.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.ProductCode == productCode, cancellationToken);
    }

    public Task<Product?> GetWithVariantsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Db.Products.Include(x => x.Variants).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetWithVariantsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var productIds = ids.Distinct().ToList();
        return await Db.Products
            .Include(x => x.Variants)
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<ProductVariant?> GetVariantAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        return Db.ProductVariants.SingleOrDefaultAsync(x => x.Id == variantId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default)
    {
        var ids = variantIds.Distinct().ToList();
        return await Db.ProductVariants.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
    }

    public Task<Color?> GetColorAsync(Guid colorId, CancellationToken cancellationToken = default)
    {
        return Db.Colors.SingleOrDefaultAsync(x => x.Id == colorId, cancellationToken);
    }

    public Task<Size?> GetSizeAsync(Guid sizeId, CancellationToken cancellationToken = default)
    {
        return Db.Sizes.SingleOrDefaultAsync(x => x.Id == sizeId, cancellationToken);
    }
}
