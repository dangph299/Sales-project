using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;

namespace Sales.Application.Features.Products.Queries;

/// <summary>
/// Handles <see cref="ListCategoriesQuery"/> by delegating to the category read service.
/// </summary>
public sealed class ListCategoriesHandler(ICategoryReadService categoryReadService)
    : IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryLookupDto>>
{
    /// <summary>
    /// Lists categories available for catalog assignment.
    /// </summary>
    /// <returns>Categories ordered by sort order, then name.</returns>
    public Task<IReadOnlyList<CategoryLookupDto>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        return categoryReadService.ListCategoriesAsync(cancellationToken);
    }
}
