namespace Sales.Domain;

/// <summary>
/// Command-side persistence contract for complete aggregate roots.
/// </summary>
public interface IRepository<T> where T : AggregateRoot<Guid>
{
    /// <summary>
    /// Loads a single aggregate by its identifier.
    /// </summary>
    /// <param name="id">Aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregate, or <see langword="null"/> when none exists.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads multiple aggregates by their identifiers in a single round trip.
    /// </summary>
    /// <param name="ids">Aggregate identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregates found for the requested identifiers.</returns>
    Task<IReadOnlyList<T>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new aggregate to be inserted when the unit of work is committed.
    /// </summary>
    /// <param name="aggregate">Aggregate to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(T aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an existing aggregate to be updated when the unit of work is committed.
    /// </summary>
    /// <param name="aggregate">Aggregate.</param>
    void Update(T aggregate);

    /// <summary>
    /// Registers an existing aggregate to be deleted when the unit of work is committed.
    /// </summary>
    /// <param name="aggregate">Aggregate.</param>
    void Delete(T aggregate);
}
