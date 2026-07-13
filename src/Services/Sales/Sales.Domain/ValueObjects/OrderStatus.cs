namespace Sales.Domain;

/// <summary>
/// lifecycle status of an <see cref="Order"/>.
/// </summary>
public enum OrderStatus
{
    /// <summary>The order has been created but confirmation has not been requested yet.</summary>
    Draft,

    /// <summary>Confirmation has been requested and Inventory has not yet responded.</summary>
    PendingInventory,

    /// <summary>Inventory reserved stock for every line and the order is confirmed.</summary>
    Confirmed,

    /// <summary>Inventory rejected the reservation request, typically due to insufficient stock.</summary>
    InventoryRejected,

    /// <summary>The order was cancelled.</summary>
    Cancelled
}
