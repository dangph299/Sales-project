namespace Inventory.Api.Models.Requests;

/// <summary>
/// HTTP query inputs for the inventory summary endpoint. All fields are optional and additive;
/// <see cref="WarehouseId"/>, <see cref="LocationId"/>, and <see cref="CompanyId"/> are reserved
/// for future filtering and are not yet applied.
/// </summary>
public sealed class InventorySummaryRequest
{
    /// <summary>Low-stock threshold override. Falls back to server configuration when omitted.</summary>
    public int? LowStockThreshold { get; init; }

    /// <summary>Reserved warehouse filter, not yet applied.</summary>
    public Guid? WarehouseId { get; init; }

    /// <summary>Reserved location filter, not yet applied.</summary>
    public Guid? LocationId { get; init; }

    /// <summary>Reserved company filter, not yet applied.</summary>
    public Guid? CompanyId { get; init; }
}
