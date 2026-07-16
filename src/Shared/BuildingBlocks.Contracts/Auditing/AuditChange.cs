namespace BuildingBlocks.Contracts;

/// <summary>
/// Field-level before/after value captured in an audit event.
/// </summary>
public sealed record AuditChange
{
    /// <summary>
    /// Gets the stable property path within the audited aggregate.
    /// </summary>
    public required string PropertyPath { get; init; }

    /// <summary>
    /// Gets the value before the change.
    /// </summary>
    public object? OldValue { get; init; }

    /// <summary>
    /// Gets the value after the change.
    /// </summary>
    public object? NewValue { get; init; }
}
