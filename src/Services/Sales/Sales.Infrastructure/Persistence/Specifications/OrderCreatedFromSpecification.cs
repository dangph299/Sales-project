using System.Linq.Expressions;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Matches orders created on or after a given instant.
/// </summary>
/// <param name="from">
/// The inclusive lower bound on the order's creation time.
/// </param>
public sealed class OrderCreatedFromSpecification(DateTimeOffset from) : Specification<Order>
{
    /// <inheritdoc/>
    public override Expression<Func<Order, bool>> ToExpression() => x => x.CreatedAt >= from;
}
