namespace BuildingBlocks.Domain;

/// <summary>
/// Marker contract for a fact raised by an aggregate. Implementations carry domain data only and
/// know nothing about transport, storage, or framework concerns.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the UTC instant at which the event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
