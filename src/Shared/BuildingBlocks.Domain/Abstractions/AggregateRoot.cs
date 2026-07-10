namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for a consistency boundary in the domain model. It buffers domain events until the
/// application or infrastructure layer persists and dispatches them.
/// </summary>
/// <typeparam name="TId">
/// The aggregate identifier type.
/// </typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new aggregate with the provided identifier.
    /// </summary>
    /// <param name="id">
    /// The aggregate identifier.
    /// </param>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>
    /// Initializes a new aggregate for ORM materialization.
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Gets the optimistic concurrency version of the aggregate. Incremented by <see cref="Touch"/>
    /// whenever the aggregate's state changes.
    /// </summary>
    public long Version { get; protected set; } = 1;

    /// <summary>
    /// Gets the UTC instant this aggregate was last changed.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the domain events raised by this aggregate since it was loaded or created.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Gets the domain events raised by this aggregate since it was loaded or created.
    /// </summary>
    /// <returns>
    /// A read-only snapshot of the buffered domain events.
    /// </returns>
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => DomainEvents;

    /// <summary>
    /// Clears all currently buffered domain events.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Buffers a domain event to be dispatched after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">
    /// The domain event describing the fact that occurred.
    /// </param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Buffers a domain event to be dispatched after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">
    /// The domain event describing the fact that occurred.
    /// </param>
    protected void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);

    /// <summary>
    /// Increments the aggregate version and updates its last-changed timestamp.
    /// </summary>
    protected void Touch()
    {
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
