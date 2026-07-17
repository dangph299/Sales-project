namespace Sales.Application.Common.Interfaces;

/// <summary>
/// Generic cache-aside port for a read-model type.
/// </summary>
public interface ICacheService<T>
{
    /// <summary>
    /// Gets a cached value by its identifier.
    /// </summary>
    /// <param name="id">Value identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached value, or <see langword="null"/> if it is not cached.</returns>
    Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a value in the cache, replacing any existing entry for the same identifier.
    /// </summary>
    /// <param name="value">Value to cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync(T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached value by its identifier, if present.
    /// </summary>
    /// <param name="id">Value identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
