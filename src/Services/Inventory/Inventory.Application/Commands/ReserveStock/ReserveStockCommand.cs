namespace Inventory.Application;

/// <summary>
/// Command that reserves stock for a Sales order confirmation event.
/// </summary>
/// <param name="EventId">Consumed integration event id.</param>
/// <param name="OrderId">Sales order identifier.</param>
/// <param name="OrderVersion">Sales order version carried by the event.</param>
/// <param name="CorrelationId">Correlation id to propagate to reply events.</param>
/// <param name="Lines">Product lines requested by Sales.</param>
public sealed record ReserveStockCommand(
    Guid EventId,
    Guid OrderId,
    long OrderVersion,
    Guid CorrelationId,
    IReadOnlyCollection<OrderLineIntegration> Lines) : IIdempotentCommand<string>
{
    /// <inheritdoc/>
    public string DuplicateResponse => "Duplicate";
}
