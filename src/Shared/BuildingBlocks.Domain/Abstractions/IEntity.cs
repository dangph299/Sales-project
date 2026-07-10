namespace BuildingBlocks.Domain;

/// <summary>
/// Marker contract for a domain object with a stable identity.
/// </summary>
/// <typeparam name="TId">
/// The identifier type.
/// </typeparam>
public interface IEntity<out TId>
    where TId : notnull
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    TId Id { get; }
}
