using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Queries;

/// <summary>
/// Query listing every category available for catalog assignment.
/// </summary>
public sealed record ListCategoriesQuery : IQuery<IReadOnlyList<CategoryLookupDto>>;
