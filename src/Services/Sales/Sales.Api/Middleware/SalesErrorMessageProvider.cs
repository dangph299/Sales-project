using BuildingBlocks.Contracts;

namespace Sales.Api.Middleware;

/// <summary>
/// Sales-specific public error descriptions. Error codes remain defined only in the shared catalog.
/// </summary>
public sealed class SalesErrorMessageProvider : DefaultErrorMessageProvider
{
    /// <inheritdoc />
    public override string GetDescription(string code, string defaultDescription)
    {
        return code switch
        {
            ErrorCodes.NotFound => "The requested sales resource was not found.",
            ErrorCodes.ConcurrencyConflict => "The sales resource was changed by another request.",
            _ => base.GetDescription(code, defaultDescription)
        };
    }
}
