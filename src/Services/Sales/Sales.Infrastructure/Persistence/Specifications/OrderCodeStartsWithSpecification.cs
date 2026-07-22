using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Matches orders whose business code starts with a search term.
/// </summary>
/// <remarks>
/// Runs against the whole table rather than whichever page the client happens to be showing, and
/// uses the unique <c>IX_orders_OrderCode</c> index. Order codes are uppercase, so the term is
/// uppercased to spare the caller having to be.
/// </remarks>
/// <param name="orderCodeSearchTerm">Whole or partial order code, matched from the start.</param>
public sealed class OrderCodeStartsWithSpecification(string orderCodeSearchTerm) : Specification<Order>
{
    /// <inheritdoc/>
    public override Expression<Func<Order, bool>> ToExpression()
    {
        var normalizedOrderCodeSearchTerm = orderCodeSearchTerm.Trim().ToUpperInvariant();
        return x => EF.Functions.Like(x.OrderCode, normalizedOrderCodeSearchTerm + "%");
    }
}
