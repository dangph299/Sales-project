namespace Sales.Domain;

/// <summary>
/// Command-side persistence contract for an aggregate type. Implementations must rehydrate complete
/// aggregates and must not expose <c>DbSet</c>, <c>IQueryable</c>, or DTOs.
/// </summary>
/// <typeparam name="T">
/// The aggregate root type this repository persists.
/// </typeparam>
public interface IRepository<T> where T : AggregateRoot<Guid>
{
    /// <summary>
    /// Loads a single aggregate by its identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the aggregate to load.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The aggregate, or <see langword="null"/> if no aggregate with that identifier exists.
    /// </returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads multiple aggregates by their identifiers in a single round trip.
    /// </summary>
    /// <param name="ids">
    /// The unique identifiers of the aggregates to load.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The aggregates found for the given identifiers. Identifiers with no matching aggregate are omitted.
    /// </returns>
    Task<IReadOnlyList<T>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new aggregate to be inserted when the unit of work is committed.
    /// </summary>
    /// <param name="aggregate">
    /// The aggregate to add.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task AddAsync(T aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an existing aggregate to be updated when the unit of work is committed.
    /// </summary>
    /// <param name="aggregate">
    /// The aggregate to update.
    /// </param>
    void Update(T aggregate);

    /// <summary>
    /// Registers an existing aggregate to be deleted when the unit of work is committed.
    /// </summary>
    /// <param name="aggregate">
    /// The aggregate to delete.
    /// </param>
    void Delete(T aggregate);
}
