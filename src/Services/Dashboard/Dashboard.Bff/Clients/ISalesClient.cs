using Dashboard.Bff.Contracts;

namespace Dashboard.Bff.Clients;

/// <summary>
/// Typed client for reading order, product, and customer data from Sales.Api.
/// </summary>
public interface ISalesClient
{
    /// <summary>Returns the most recently created orders, newest first.</summary>
    Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersAsync(int take, CancellationToken cancellationToken);

    /// <summary>Returns orders created within the given date range, used to render the order chart.</summary>
    Task<IReadOnlyList<OrderChartPointDto>> GetOrdersInRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>Returns the total number of orders.</summary>
    Task<long> GetOrderCountAsync(CancellationToken cancellationToken);

    /// <summary>Returns the number of orders currently pending inventory allocation.</summary>
    Task<long> GetPendingOrderCountAsync(CancellationToken cancellationToken);

    /// <summary>Returns the total number of products.</summary>
    Task<long> GetProductCountAsync(CancellationToken cancellationToken);

    /// <summary>Returns the number of products currently published.</summary>
    Task<long> GetPublishedProductCountAsync(CancellationToken cancellationToken);

    /// <summary>Returns the total number of customers.</summary>
    Task<long> GetCustomerCountAsync(CancellationToken cancellationToken);
}
