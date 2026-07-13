namespace BuildingBlocks.Contracts;

/// <summary>
/// Base record for integration events that carry their own identity and occurrence time.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
