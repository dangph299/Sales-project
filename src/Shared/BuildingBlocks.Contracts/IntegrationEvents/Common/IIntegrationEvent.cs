namespace BuildingBlocks.Contracts;

/// <summary>
/// Marker for integration event payloads exchanged across service boundaries.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }
}
