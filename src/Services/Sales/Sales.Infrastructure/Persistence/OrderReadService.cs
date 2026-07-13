using Microsoft.EntityFrameworkCore;
using Sales.Application;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Read-side order lookup service for query handlers.
/// Search filters are composed from <see cref="Specification{T}"/> rules.
/// </summary>
public sealed class OrderReadService(SalesDbContext db) : IOrderReadService
{
    /// <inheritdoc/>
    public async Task<OrderDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.Orders.Include(x => x.Lines).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return order?.ToDto();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<OrderDto>> SearchAsync(DateTimeOffset? from, DateTimeOffset? to, string? customer, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var query = db.Orders.Include(x => x.Lines).AsNoTracking();

        Specification<Order>? spec = null;
        if (from is not null) spec = Compose(spec, new OrderCreatedFromSpecification(from.Value));
        if (to is not null) spec = Compose(spec, new OrderCreatedToSpecification(to.Value));
        if (!string.IsNullOrWhiteSpace(customer)) spec = Compose(spec, new OrderCustomerMatchesSpecification(customer));
        if (spec is not null) query = query.Where(spec.ToExpression());

        var total = await query.LongCountAsync(ct);
        var orders = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(orders.Select(x => x.ToDto()).ToArray(), page, pageSize, total);
    }

    private static Specification<Order> Compose(Specification<Order>? current, Specification<Order> next) => current is null ? next : current.And(next);
}
