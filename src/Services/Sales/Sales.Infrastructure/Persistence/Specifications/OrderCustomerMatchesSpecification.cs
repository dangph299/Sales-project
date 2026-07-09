using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Matches orders whose customer name contains the given substring, or whose customer phone
/// contains the given value's digits.
/// </summary>
/// <param name="customer">
/// The search value, matched against both name and phone.
/// </param>
public sealed class OrderCustomerMatchesSpecification(string customer) : Specification<Order>
{
    /// <inheritdoc/>
    public override Expression<Func<Order, bool>> ToExpression()
    {
        var value = customer.Trim();
        var phone = new string(value.Where(char.IsDigit).ToArray());
        return x => EF.Functions.ILike(x.CustomerName, $"%{value}%") || (phone != "" && x.CustomerPhone.Contains(phone));
    }
}
