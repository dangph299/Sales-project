namespace Inventory.Application;

/// <summary>
/// Command that releases stock for a Sales order undo-confirmation event.
/// </summary>
/// <param name="EventId">Consumed integration event id.</param>
/// <param name="OrderId">Sales order identifier.</param>
/// <param name="OrderVersion">Sales order version carried by the event.</param>
/// <param name="CorrelationId">Correlation id to propagate to reply events.</param>
public sealed record ReleaseStockCommand(Guid EventId, Guid OrderId, long OrderVersion, Guid CorrelationId) : IIdempotentCommand<string>
{
    /// <inheritdoc/>
    public string DuplicateResponse => "Duplicate";
}
