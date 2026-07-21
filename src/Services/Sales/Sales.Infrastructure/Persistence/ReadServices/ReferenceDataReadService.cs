using Microsoft.EntityFrameworkCore;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;

namespace Sales.Infrastructure;

/// <summary>
/// Read-side lookup for the seeded color and size reference entities.
/// </summary>
public sealed class ReferenceDataReadService(SalesDbContext db) : IReferenceDataReadService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ColorLookupDto>> ListColorsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Colors.AsNoTracking()
            .OrderBy(color => color.ColorCode)
            .Select(color => new ColorLookupDto(color.Id, color.ColorCode, color.Name, color.HexCode))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SizeLookupDto>> ListSizesAsync(CancellationToken cancellationToken = default)
    {
        return await db.Sizes.AsNoTracking()
            .OrderBy(size => size.SortOrder)
            .Select(size => new SizeLookupDto(size.Id, size.Code, size.Name, size.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
