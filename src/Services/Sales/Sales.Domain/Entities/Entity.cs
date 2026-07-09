namespace Sales.Domain;

/// <summary>
/// Base class for any domain object that has a stable identity, whether it is an
/// <see cref="AggregateRoot"/> or an entity owned by one (for example <see cref="OrderLine"/>).
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets the unique identifier of this entity.
    /// </summary>
    public Guid Id { get; protected set; }
}
