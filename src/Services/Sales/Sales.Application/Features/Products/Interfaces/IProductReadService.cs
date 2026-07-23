using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Interfaces;

/// <summary>
/// Query-side read port for products, implemented directly against the database without going
/// through the command-side repository/aggregate.
/// </summary>
public interface IProductReadService
{
    /// <summary>
    /// Gets a single published product by its identifier, for public catalog reads.
    /// </summary>
    /// <param name="id">Product identifier.</param>
    /// <returns>Product, or <see langword="null"/> if none exists or it is not published.</returns>
    Task<ProductDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by its identifier regardless of publication status, for returning the result
    /// of a write. Command handlers must not read their own result back through
    /// <see cref="GetAsync"/>: a Draft or Discontinued product it just persisted would come back
    /// <see langword="null"/> and be reported as missing.
    /// </summary>
    /// <param name="id">Product identifier.</param>
    /// <returns>Product, or <see langword="null"/> if none exists.</returns>
    Task<ProductDto?> GetForWriteResultAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name.
    /// </summary>
    /// <param name="name">An optional substring to match against the product's name.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum page size.</param>
    /// <returns>A page of matching products.</returns>
    Task<PagedResult<ProductDto>> SearchAsync(
        string? productCode,
        string? name,
        Guid? categoryId,
        string? sku,
        Guid? colorId,
        Guid? sizeId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(name, page, pageSize, cancellationToken);
    }

    /// <summary>
    /// Searches products by name.
    /// </summary>
    Task<PagedResult<ProductDto>> SearchAsync(
        string? name,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches product variants with owning product details for variant-oriented screens.
    /// </summary>
    Task<PagedResult<ProductVariantLookupDto>> SearchVariantsAsync(
        string? productCode,
        string? productName,
        string? sku,
        string? variantStatus,
        string? sortBy,
        string? sortDirection,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
