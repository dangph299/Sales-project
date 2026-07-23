using Microsoft.EntityFrameworkCore;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;

namespace Sales.Infrastructure;

/// <summary>
/// Read-side lookup for the Category aggregate.
/// </summary>
public sealed class CategoryReadService(SalesDbContext db) : ICategoryReadService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<CategoryLookupDto>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        // Soft-deleted categories are already excluded by the Category query filter.
        return await db.Categories.AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryLookupDto(
                category.Id,
                category.CategoryCode,
                category.Name,
                category.Description,
                category.ParentCategoryId,
                category.SortOrder,
                category.Status.ToString(),
                db.Products.Count(product => product.CategoryId == category.Id),
                category.CreatedAt,
                category.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> HasDependentsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        // Both query filters exclude soft-deleted rows, so this only counts dependents that are still live.
        return await db.Categories.AsNoTracking()
                   .AnyAsync(category => category.ParentCategoryId == categoryId, cancellationToken)
               || await db.Products.AsNoTracking()
                   .AnyAsync(product => product.CategoryId == categoryId, cancellationToken);
    }
}
