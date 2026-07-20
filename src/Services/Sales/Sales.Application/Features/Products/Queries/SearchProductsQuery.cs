using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Queries;

/// <summary>
/// Query to search products by catalog and variant attributes.
/// </summary>
public sealed record SearchProductsQuery(
    string? ProductCode,
    string? Name,
    Guid? CategoryId,
    string? Sku,
    Guid? ColorId,
    Guid? SizeId,
    string? Status,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ProductDto>>;
