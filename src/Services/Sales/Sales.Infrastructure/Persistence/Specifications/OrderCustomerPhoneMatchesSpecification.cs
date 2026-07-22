using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Matches orders whose own customer phone snapshot starts or ends with a search term.
/// </summary>
/// <remarks>
/// Both modes compile to an indexable <c>LIKE 'digits%'</c>. A suffix search runs against the
/// reversed column with the reversed term rather than <c>LIKE '%digits'</c>, because a leading
/// wildcard cannot use a B-tree index. The search term is digits-only by the time it arrives, so it
/// can never carry a <c>%</c> or <c>_</c> that would need escaping.
/// </remarks>
/// <param name="normalizedCustomerPhoneSearchTerm">Search term already reduced to digits via <see cref="CustomerPhoneNormalizer.NormalizeSearchTerm"/>.</param>
/// <param name="customerPhoneMatchMode">Which end of the phone number the term must match.</param>
public sealed class OrderCustomerPhoneMatchesSpecification(
    string normalizedCustomerPhoneSearchTerm,
    OrderCustomerPhoneMatchMode customerPhoneMatchMode) : Specification<Order>
{
    /// <inheritdoc/>
    public override Expression<Func<Order, bool>> ToExpression()
    {
        if (customerPhoneMatchMode == OrderCustomerPhoneMatchMode.Suffix)
        {
            var reversedCustomerPhoneSearchTerm = CustomerPhoneNormalizer.Reverse(normalizedCustomerPhoneSearchTerm);
            return x => EF.Functions.Like(x.ReversedCustomerPhone, reversedCustomerPhoneSearchTerm + "%");
        }

        return x => EF.Functions.Like(x.NormalizedCustomerPhone, normalizedCustomerPhoneSearchTerm + "%");
    }
}
