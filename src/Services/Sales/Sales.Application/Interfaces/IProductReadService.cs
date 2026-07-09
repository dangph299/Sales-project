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
    /// <param name="id">
    /// The unique identifier of the product to look up.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The product, or <see langword="null"/> if none exists.
    /// </returns>
    Task<ProductDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name.
    /// </summary>
    /// <param name="name">
    /// An optional substring to match against the product's name.
    /// </param>
    /// <param name="page">
    /// The 1-based page number to return.
    /// </param>
    /// <param name="pageSize">
    /// The maximum number of items per page.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// A page of matching products.
    /// </returns>
    Task<PagedResult<ProductDto>> SearchAsync(string? name, int page, int pageSize, CancellationToken cancellationToken = default);
}
