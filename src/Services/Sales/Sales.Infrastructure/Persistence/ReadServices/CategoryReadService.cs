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
        return await db.Categories.AsNoTracking()
            .Where(category => !category.IsDelete)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryLookupDto(
                category.Id,
                category.CategoryCode,
                category.Name,
                category.Description,
                category.ParentCategoryId,
                category.SortOrder,
                category.Status.ToString()))
            .ToListAsync(cancellationToken);
    }
}
