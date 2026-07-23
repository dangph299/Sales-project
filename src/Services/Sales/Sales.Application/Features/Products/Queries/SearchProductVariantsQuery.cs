using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Queries;

/// <summary>
/// Query to search and page product variants with their owning product details.
/// </summary>
public sealed record SearchProductVariantsQuery(
    string? ProductCode,
    string? ProductName,
    string? Sku,
    string? VariantStatus,
    string? SortBy,
    string? SortDirection,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ProductVariantLookupDto>>;
