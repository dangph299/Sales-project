using MediatR;

namespace Sales.Application;

/// <summary>
/// Query to search products by name.
/// </summary>
/// <param name="Name">
/// An optional substring to match against the product's name.
/// </param>
/// <param name="Page">
/// The 1-based page number to return. Defaults to 1.
/// </param>
/// <param name="PageSize">
/// The maximum number of items per page. Defaults to 20.
/// </param>
public sealed record SearchProducts(string? Name, int Page = 1, int PageSize = 20) : IRequest<PagedResult<ProductDto>>;
