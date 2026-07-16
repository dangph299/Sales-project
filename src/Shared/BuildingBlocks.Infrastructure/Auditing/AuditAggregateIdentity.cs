namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Identifies the aggregate that should own one or more entity changes in an audit event.
/// </summary>
public sealed record AuditAggregateIdentity(
    string EntityType,
    string EntityId,
    Guid? AggregateId,
    string PropertyPrefix);
