namespace Dashboard.Bff.Clients.Payloads;

/// <summary>
/// Narrow shape of a single item from Sales.Api's <c>GET /api/orders</c> list endpoint — only the
/// fields the Dashboard BFF reads.
/// </summary>
public sealed record OrderListItemPayload(
    Guid Id,
    string OrderCode,
    string CustomerName,
    string Status,
    int TotalQuantity,
    decimal Total,
    DateTimeOffset CreatedAt);
