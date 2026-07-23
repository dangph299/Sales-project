namespace Dashboard.Bff.Contracts;

/// <summary>
/// Summary of a recently placed Sales order.
/// </summary>
/// <param name="Id">Order identifier.</param>
/// <param name="OrderCode">Human-readable order code.</param>
/// <param name="CustomerName">Name of the customer who placed the order.</param>
/// <param name="Status">Current order status.</param>
/// <param name="TotalQuantity">Total quantity of items on the order.</param>
/// <param name="Total">Total order amount.</param>
/// <param name="CreatedAt">Timestamp the order was created.</param>
public sealed record RecentOrderDto(
    Guid Id,
    string OrderCode,
    string CustomerName,
    string Status,
    int TotalQuantity,
    decimal Total,
    DateTimeOffset CreatedAt);
