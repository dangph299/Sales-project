using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Interfaces;

/// <summary>
/// Read-side port for the Category aggregate. Kept separate from the color/size reference port
/// because Category owns its own lifecycle and write model.
/// </summary>
public interface ICategoryReadService
{
    /// <summary>
    /// Lists categories available for catalog assignment, ordered by sort order then name.
    /// </summary>
    Task<IReadOnlyList<CategoryLookupDto>> ListCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports whether any live child category or product still points at this category.
    /// </summary>
    Task<bool> HasDependentsAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
