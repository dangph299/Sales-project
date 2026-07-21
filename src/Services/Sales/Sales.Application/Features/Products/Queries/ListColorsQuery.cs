using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Queries;

/// <summary>
/// Query listing every color available to product variants.
/// </summary>
public sealed record ListColorsQuery : IQuery<IReadOnlyList<ColorLookupDto>>;
