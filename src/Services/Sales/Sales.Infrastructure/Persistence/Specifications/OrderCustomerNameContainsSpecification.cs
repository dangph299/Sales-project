using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Matches orders whose own customer name snapshot contains a keyword.
/// </summary>
/// <remarks>
/// Reads the order's own snapshot, so renaming or deleting a customer never changes which orders a
/// past search finds. The match is case-insensitive.
/// </remarks>
/// <param name="customerNameSearchTerm">Keyword to match anywhere within the order's customer name.</param>
public sealed class OrderCustomerNameContainsSpecification(string customerNameSearchTerm) : Specification<Order>
{
    /// <inheritdoc/>
    public override Expression<Func<Order, bool>> ToExpression()
    {
        var trimmedCustomerNameSearchTerm = customerNameSearchTerm.Trim();
        return x => EF.Functions.ILike(x.CustomerName, $"%{trimmedCustomerNameSearchTerm}%");
    }
}
