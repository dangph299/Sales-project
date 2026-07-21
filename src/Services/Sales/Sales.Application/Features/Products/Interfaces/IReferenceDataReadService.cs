using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Interfaces;

/// <summary>
/// Read-side port for the seeded color and size reference entities. Clients resolve business
/// identity from the stable code and submit the persistence identifier returned here, so no
/// reference-data identifier needs to be hardcoded outside the database.
/// </summary>
public interface IReferenceDataReadService
{
    /// <summary>
    /// Lists all colors ordered by code.
    /// </summary>
    Task<IReadOnlyList<ColorLookupDto>> ListColorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all sizes ordered by their seeded sort order.
    /// </summary>
    Task<IReadOnlyList<SizeLookupDto>> ListSizesAsync(CancellationToken cancellationToken = default);
}
