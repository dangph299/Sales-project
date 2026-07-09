namespace Sales.Domain;

/// <summary>
/// Marker contract for a fact raised by an aggregate. Implementations carry business data only and
/// know nothing about how they are transported (Kafka topics, etc.).
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the UTC instant at which the event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
