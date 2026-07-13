namespace Sales.Application;

/// <summary>
/// Query-side read port for products, implemented directly against the database without going
/// through the command-side repository/aggregate.
/// </summary>
public interface IProductReadService
{
    /// <summary>
    /// Gets a single product by its identifier.
    /// </summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Product, or <see langword="null"/> if none exists.</returns>
    Task<ProductDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name.
    /// </summary>
    /// <param name="name">An optional substring to match against the product's name.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A page of matching products.</returns>
    Task<PagedResult<ProductDto>> SearchAsync(string? name, int page, int pageSize, CancellationToken cancellationToken = default);
}
