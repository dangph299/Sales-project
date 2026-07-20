using System.Linq.Expressions;

namespace Sales.Domain;

/// <summary>
/// Matches customers that can create orders and have not been soft-deleted.
/// </summary>
public sealed class ActiveCustomerSpecification : Specification<Customer>
{
    /// <inheritdoc/>
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return x => x.Status == ECustomerStatus.Normal && !x.IsDelete;
    }
}
