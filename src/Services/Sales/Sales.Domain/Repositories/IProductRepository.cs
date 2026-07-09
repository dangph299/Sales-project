namespace Sales.Domain;

/// <summary>
/// Command-side persistence contract for <see cref="Product"/>, adding the SKU lookup used to
/// enforce SKU uniqueness.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Loads a single product by its SKU.
    /// </summary>
    /// <param name="sku">
    /// The SKU to look up.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The product with the given SKU, or <see langword="null"/> if none exists.
    /// </returns>
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
}
