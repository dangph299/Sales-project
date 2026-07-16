using System.Diagnostics;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Default audit context for background work or services without a richer ambient context.
/// </summary>
public sealed class SystemAuditContextAccessor : IAuditContextAccessor
{
    /// <inheritdoc/>
    public string? ActorId => "system";

    /// <inheritdoc/>
    public string? ActorName => "system";

    /// <inheritdoc/>
    public string? CorrelationId => Activity.Current?.TraceId.ToHexString();

    /// <inheritdoc/>
    public string? CausationId => null;

    /// <inheritdoc/>
    public string? TraceId => Activity.Current?.TraceId.ToHexString();
}
