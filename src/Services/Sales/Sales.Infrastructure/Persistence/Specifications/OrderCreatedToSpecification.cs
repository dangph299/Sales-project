using System.Linq.Expressions;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Matches orders created strictly before a given instant.
/// </summary>
/// <param name="to">
/// The exclusive upper bound on the order's creation time.
/// </param>
public sealed class OrderCreatedToSpecification(DateTimeOffset to) : Specification<Order>
{
    /// <inheritdoc/>
    public override Expression<Func<Order, bool>> ToExpression() => x => x.CreatedAt < to;
}
