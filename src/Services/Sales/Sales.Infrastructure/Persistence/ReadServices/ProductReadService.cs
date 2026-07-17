using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Read-side product lookup service for query handlers.
/// Not cached itself — see <see cref="CachedProductReadService"/> for the cache-aside decorator
/// registered as <see cref="IProductReadService"/>.
/// </summary>
public sealed class ProductReadService(SalesDbContext db, IMapper mapper) : IProductReadService
{
    /// <inheritdoc/>
    public async Task<ProductDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var activeProduct = new ActiveProductSpecification();
        var product = await db.Products.AsNoTracking()
            .Where(activeProduct.ToExpression())
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return product is null ? null : mapper.Map<ProductDto>(product);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<ProductDto>> SearchAsync(string? name, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var query = db.Products.AsNoTracking().Where(x => !x.IsDelete);
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => EF.Functions.ILike(x.Name, $"%{name.Trim()}%"));
        var total = await query.LongCountAsync(ct);
        var products = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(mapper.Map<ProductDto[]>(products), page, pageSize, total);
    }
}
