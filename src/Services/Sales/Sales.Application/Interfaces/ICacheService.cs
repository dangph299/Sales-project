namespace Sales.Application;

/// <summary>
/// Generic cache-aside port for a read-model type.
/// </summary>
/// <typeparam name="T">
/// The type of value cached.
/// </typeparam>
public interface ICacheService<T>
{
    /// <summary>
    /// Gets a cached value by its identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the value to look up.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The cached value, or <see langword="null"/> if it is not cached.
    /// </returns>
    Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a value in the cache, replacing any existing entry for the same identifier.
    /// </summary>
    /// <param name="value">
    /// The value to cache.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task SetAsync(T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached value by its identifier, if present.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the value to remove.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
