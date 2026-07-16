namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Provides request or message context used to stamp audit events without coupling audit code to HTTP or Kafka.
/// </summary>
public interface IAuditContextAccessor
{
    /// <summary>Gets the actor identifier.</summary>
    string? ActorId { get; }

    /// <summary>Gets the actor display name.</summary>
    string? ActorName { get; }

    /// <summary>Gets the correlation identifier.</summary>
    string? CorrelationId { get; }

    /// <summary>Gets the causation identifier.</summary>
    string? CausationId { get; }

    /// <summary>Gets the current trace identifier.</summary>
    string? TraceId { get; }
}
