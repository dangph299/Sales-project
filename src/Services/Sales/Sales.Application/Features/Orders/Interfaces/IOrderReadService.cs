using Sales.Application.Features.Orders.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Interfaces;

/// <summary>
/// Query-side read port for orders, implemented directly against the database without going
/// through the command-side repository/aggregate.
/// </summary>
public interface IOrderReadService
{
    /// <summary>
    /// Gets a single order by its identifier.
    /// </summary>
    /// <param name="id">Order identifier.</param>
    /// <returns>Order, or <see langword="null"/> if none exists.</returns>
    Task<OrderDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches orders by creation date range, customer name/phone, and/or status.
    /// </summary>
    /// <param name="from">An optional inclusive lower bound on <c>CreatedAt</c>.</param>
    /// <param name="to">An optional inclusive upper bound on <c>CreatedAt</c>.</param>
    /// <param name="customer">An optional substring to match against the order's customer name or phone.</param>
    /// <param name="status">An optional status the order must currently be in.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum page size.</param>
    /// <returns>A page of matching orders.</returns>
    Task<PagedResult<OrderDto>> SearchAsync(DateTimeOffset? from, DateTimeOffset? to, string? customer, OrderStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
}
