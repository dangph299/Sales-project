using BuildingBlocks.Contracts;

namespace Inventory.Api.Middleware;

/// <summary>
/// Inventory-specific public error descriptions. Error codes remain defined only in the shared catalog.
/// </summary>
public sealed class InventoryErrorMessageProvider : DefaultErrorMessageProvider
{
    /// <inheritdoc />
    public override string GetDescription(string code, string defaultDescription)
    {
        return code switch
        {
            ErrorCodes.NotFound => "The requested inventory resource was not found.",
            ErrorCodes.ConcurrencyConflict => "Inventory was changed by another operation. Please retry.",
            ErrorCodes.StaleReservation => "The reservation event is older than the current inventory state.",
            _ => base.GetDescription(code, defaultDescription)
        };
    }
}
