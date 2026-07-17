using BuildingBlocks.Infrastructure;
using Sales.Application.Common.Interfaces;

namespace Sales.Infrastructure;

/// <summary>
/// Adapts the Sales execution context to the shared audit context.
/// </summary>
public sealed class SalesAuditContextAccessor(IExecutionContext executionContext) : IAuditContextAccessor
{
    /// <inheritdoc/>
    public string? ActorId => executionContext.Actor;

    /// <inheritdoc/>
    public string? ActorName => executionContext.Actor;

    /// <inheritdoc/>
    public string? CorrelationId => executionContext.CorrelationId.ToString();

    /// <inheritdoc/>
    public string? CausationId => null;

    /// <inheritdoc/>
    public string? TraceId => System.Diagnostics.Activity.Current?.TraceId.ToHexString();
}
