using System.Linq.Expressions;

namespace Sales.Domain;

/// <summary>
/// Matches products that are enabled for ordering and have not been soft-deleted.
/// </summary>
public sealed class ActiveProductSpecification : Specification<Product>
{
    /// <inheritdoc/>
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return x => x.IsActive && !x.IsDelete;
    }
}
