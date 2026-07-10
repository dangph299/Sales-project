namespace BuildingBlocks.Domain;

/// <summary>
/// Base record for concrete domain events, stamping the occurrence time at creation.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc/>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
