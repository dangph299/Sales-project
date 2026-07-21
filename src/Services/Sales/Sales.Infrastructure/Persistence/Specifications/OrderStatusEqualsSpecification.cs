using System.Linq.Expressions;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Matches orders currently in a given status.
/// </summary>
/// <param name="status">Status the order must be in.</param>
public sealed class OrderStatusEqualsSpecification(OrderStatus status) : Specification<Order>
{
    /// <inheritdoc/>
    public override Expression<Func<Order, bool>> ToExpression() => x => x.Status == status;
}
