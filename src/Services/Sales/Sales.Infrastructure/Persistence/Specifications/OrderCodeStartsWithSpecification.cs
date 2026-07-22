using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Matches orders whose business code starts with a search term.
/// </summary>
/// <remarks>
/// The term is matched case-insensitively, so the caller may pass it in any case.
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
