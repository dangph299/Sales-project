using MediatR;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;

namespace Sales.Application.Features.Products.Queries;

/// <summary>
/// Handles paged product variant lookup queries.
/// </summary>
public sealed class SearchProductVariantsHandler(IProductReadService readService)
    : IRequestHandler<SearchProductVariantsQuery, PagedResult<ProductVariantLookupDto>>
{
    /// <inheritdoc/>
    public Task<PagedResult<ProductVariantLookupDto>> Handle(
        SearchProductVariantsQuery request,
        CancellationToken cancellationToken)
    {
        return readService.SearchVariantsAsync(
            request.ProductCode,
            request.ProductName,
            request.Sku,
            request.VariantStatus,
            request.SortBy,
            request.SortDirection,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
