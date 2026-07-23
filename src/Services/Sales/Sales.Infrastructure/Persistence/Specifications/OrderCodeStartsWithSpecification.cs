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
        // The term is a literal prefix, so its own LIKE metacharacters must not act as wildcards: a
        // user typing "%" or "_" is searching for those characters, not for anything. Only the
        // trailing "%" we append here is a wildcard.
        var escapedOrderCodeSearchTerm = LikePatternEscaper.Escape(normalizedOrderCodeSearchTerm);
        return x => EF.Functions.Like(x.OrderCode, escapedOrderCodeSearchTerm + "%", LikePatternEscaper.EscapeCharacter);
    }
}
