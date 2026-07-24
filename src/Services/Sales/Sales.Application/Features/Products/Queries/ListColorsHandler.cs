using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;

namespace Sales.Application.Features.Products.Queries;

/// <summary>
/// Handles <see cref="ListColorsQuery"/> by delegating to the reference-data read service.
/// </summary>
public sealed class ListColorsHandler(IReferenceDataReadService referenceDataReadService)
    : IQueryHandler<ListColorsQuery, IReadOnlyList<ColorLookupDto>>
{
    /// <summary>
    /// Lists every color available to product variants.
    /// </summary>
    /// <returns>Colors ordered by code.</returns>
    public Task<IReadOnlyList<ColorLookupDto>> Handle(ListColorsQuery request, CancellationToken cancellationToken)
    {
        return referenceDataReadService.ListColorsAsync(cancellationToken);
    }
}
