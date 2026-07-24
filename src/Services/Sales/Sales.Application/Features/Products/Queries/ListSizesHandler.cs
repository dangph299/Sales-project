using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;

namespace Sales.Application.Features.Products.Queries;

/// <summary>
/// Handles <see cref="ListSizesQuery"/> by delegating to the reference-data read service.
/// </summary>
public sealed class ListSizesHandler(IReferenceDataReadService referenceDataReadService)
    : IQueryHandler<ListSizesQuery, IReadOnlyList<SizeLookupDto>>
{
    /// <summary>
    /// Lists every size available to product variants.
    /// </summary>
    /// <returns>Sizes ordered by their seeded sort order.</returns>
    public Task<IReadOnlyList<SizeLookupDto>> Handle(ListSizesQuery request, CancellationToken cancellationToken)
    {
        return referenceDataReadService.ListSizesAsync(cancellationToken);
    }
}
