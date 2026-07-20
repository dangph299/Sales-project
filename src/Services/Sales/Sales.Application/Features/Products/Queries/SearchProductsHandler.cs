using MediatR;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;

namespace Sales.Application.Features.Products.Queries;

/// <summary>
/// Handles <see cref="SearchProducts"/> by delegating to the product read service.
/// </summary>
public sealed class SearchProductsHandler(IProductReadService readService) : IRequestHandler<SearchProducts, PagedResult<ProductDto>>
{
    /// <summary>
    /// Searches products matching the given criteria.
    /// </summary>
    /// <param name="request">Query describing the search criteria and paging.</param>
    /// <returns>A page of matching products.</returns>
    public async Task<PagedResult<ProductDto>> Handle(SearchProducts request, CancellationToken ct)
    {
        return await readService.SearchAsync(request.Name, request.Page, request.PageSize, ct);
    }
}
