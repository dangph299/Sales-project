namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for domain objects with a stable identity.
/// </summary>
public abstract class Entity<TId> : IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Initializes a new entity with the provided identifier.
    /// </summary>
    /// <param name="id">Entity identifier.</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Initializes a new entity for ORM materialization.
    /// </summary>
    protected Entity()
    {
    }

    /// <inheritdoc/>
    public TId Id { get; protected set; } = default!;
}
