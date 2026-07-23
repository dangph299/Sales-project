namespace Dashboard.Bff.Contracts;

/// <summary>
/// Headline metrics summarized across Sales orders, customers, and products.
/// </summary>
/// <param name="OrderTotal">Total number of orders.</param>
/// <param name="PendingOrderCount">Number of orders currently pending.</param>
/// <param name="RevenueToday">Revenue recognized so far today.</param>
/// <param name="CustomerTotal">Total number of customers.</param>
/// <param name="ProductTotal">Total number of products.</param>
/// <param name="PublishedProductCount">Number of products currently published.</param>
public sealed record DashboardMetrics(
    long OrderTotal,
    long PendingOrderCount,
    decimal RevenueToday,
    long CustomerTotal,
    long ProductTotal,
    long PublishedProductCount);
