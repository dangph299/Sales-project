using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Features.Orders.DTOs;
using Sales.Application.Features.Orders.Interfaces;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Read-side order lookup service for query handlers.
/// Search filters are composed from <see cref="Specification{T}"/> rules.
/// </summary>
/// <remarks>
/// Every value this returns, including the customer's name and contact details, comes from the
/// order row. The customer table is never joined, so an order keeps showing and matching the
/// details it was placed with even after that customer is renamed, re-numbered or soft-deleted.
/// </remarks>
public sealed class OrderReadService(SalesDbContext db, IMapper mapper) : IOrderReadService
{
    /// <inheritdoc/>
    public async Task<OrderDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.Orders.Include(x => x.Lines).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return order is null ? null : mapper.Map<OrderDto>(order);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<OrderDto>> SearchAsync(
        string? orderNumber,
        string? customerName,
        string? customerPhone,
        DateTimeOffset? from,
        DateTimeOffset? to,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var query = db.Orders.Include(x => x.Lines).AsNoTracking();

        Specification<Order>? spec = null;
        if (from is not null) spec = Compose(spec, new OrderCreatedFromSpecification(from.Value));
        if (to is not null) spec = Compose(spec, new OrderCreatedToSpecification(to.Value));
        if (!string.IsNullOrWhiteSpace(orderNumber)) spec = Compose(spec, new OrderCodeStartsWithSpecification(orderNumber));
        if (!string.IsNullOrWhiteSpace(customerName)) spec = Compose(spec, new OrderCustomerNameContainsSpecification(customerName));
        if (status is not null) spec = Compose(spec, new OrderStatusEqualsSpecification(status.Value));

        // Normalized here rather than by the caller: the client sends the phone exactly as the user
        // typed it and never has to know about the normalized or reversed columns. A term with no
        // digits at all is rejected by SearchOrdersValidator before reaching this point.
        var normalizedCustomerPhoneSearchTerm = CustomerPhoneNormalizer.NormalizeSearchTerm(customerPhone);
        if (normalizedCustomerPhoneSearchTerm.Length > 0)
        {
            spec = Compose(spec, new OrderCustomerPhoneMatchesSpecification(normalizedCustomerPhoneSearchTerm));
        }

        if (spec is not null) query = query.Where(spec.ToExpression());

        var total = await query.LongCountAsync(ct);
        var orders = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(mapper.Map<OrderDto[]>(orders), page, pageSize, total);
    }

    private static Specification<Order> Compose(Specification<Order>? current, Specification<Order> next) => current is null ? next : current.And(next);
}
