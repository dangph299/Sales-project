using System.Linq.Expressions;
using BuildingBlocks.Domain.PhoneNumbers;
using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Matches orders whose own customer phone snapshot starts with or ends with a search term.
/// </summary>
/// <remarks>
/// The user is not asked which end they remembered: an order matches if either end does, and an
/// order matching both ends is returned once.
/// </remarks>
/// <param name="normalizedCustomerPhoneSearchTerm">Search term already reduced to digits via <see cref="PhoneNumberNormalizer.NormalizeSearchTerm"/>.</param>
public sealed class OrderCustomerPhoneMatchesSpecification(string normalizedCustomerPhoneSearchTerm)
    : Specification<Order>
{
    /// <inheritdoc/>
    public override Expression<Func<Order, bool>> ToExpression()
    {
        // Reversing the term rather than the column keeps both halves a plain prefix match.
        var reversedCustomerPhoneSearchTerm = PhoneNumberNormalizer.Reverse(normalizedCustomerPhoneSearchTerm);

        return x => EF.Functions.Like(x.NormalizedCustomerPhone, normalizedCustomerPhoneSearchTerm + "%")
            || EF.Functions.Like(x.ReversedCustomerPhone, reversedCustomerPhoneSearchTerm + "%");
    }
}
