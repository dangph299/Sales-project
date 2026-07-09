namespace Sales.Domain;

/// <summary>
/// Base class for a consistency boundary in the domain model. Tracks the aggregate's optimistic
/// concurrency version and buffers domain events raised by its behavior until they are dispatched.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the optimistic concurrency version of the aggregate. Incremented by <see cref="Touch"/>
    /// whenever the aggregate's state changes.
    /// </summary>
    public long Version { get; protected set; } = 1;

    /// <summary>
    /// Gets the domain events raised by this aggregate since it was loaded or created, in the order
    /// they were raised.
    /// </summary>
    /// <returns>
    /// A read-only snapshot of the buffered domain events.
    /// </returns>
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    /// <summary>
    /// Clears all domain events currently buffered on this aggregate, typically called after they
    /// have been dispatched.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Buffers a domain event to be dispatched after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">
    /// The domain event describing the fact that occurred.
    /// </param>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Increments the aggregate's <see cref="Version"/>. Called by aggregate behavior whenever it
    /// mutates state, so optimistic concurrency checks can detect concurrent writes.
    /// </summary>
    protected void Touch() => Version++;
}
