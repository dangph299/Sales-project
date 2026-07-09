namespace Sales.Domain;

/// <summary>
/// Base record for concrete domain events, stamping the occurrence time at creation so callers do
/// not need to set it explicitly.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Gets the UTC instant at which the event occurred. Defaults to the moment the record is created.
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
