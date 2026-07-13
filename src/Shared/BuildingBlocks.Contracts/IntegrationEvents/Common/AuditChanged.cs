namespace BuildingBlocks.Contracts;

/// <summary>
/// Integration event recording that an entity was created, updated, or deleted, published to
/// AuditLog for a permanent audit trail.
/// </summary>
/// <param name="Entity">Kind of entity that changed, for example <c>"Product"</c>.</param>
/// <param name="EntityId">Identifier of the entity that changed.</param>
/// <param name="Action">Action performed, for example <c>"Created"</c> or <c>"Updated"</c>.</param>
/// <param name="Changes">Individual field-level changes.</param>
public sealed record AuditChanged(
    string Entity,
    string EntityId,
    string Action,
    IReadOnlyCollection<AuditChange> Changes) : IntegrationEventBase;

/// <summary>
/// A single field-level change captured as part of an <see cref="AuditChanged"/> event.
/// </summary>
/// <param name="Field">Changed field name.</param>
/// <param name="DisplayName">An optional human-readable label for the field, for display purposes.</param>
/// <param name="OldValue">Field's value before the change, or <see langword="null"/> if it did not previously exist.</param>
/// <param name="NewValue">Field's value after the change, or <see langword="null"/> if it was removed.</param>
/// <param name="DataType">An optional hint of the field's data type (for example <c>"string"</c>, <c>"decimal"</c>), used by consumers rendering the change.</param>
public sealed record AuditChange(
    string Field,
    string? DisplayName,
    object? OldValue,
    object? NewValue,
    string? DataType = null);
