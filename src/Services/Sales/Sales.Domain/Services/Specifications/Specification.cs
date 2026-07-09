using System.Linq.Expressions;

namespace Sales.Domain;

/// <summary>
/// Base class for a reusable, composable query rule for <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">
/// The type the specification's rule applies to.
/// </typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    /// <inheritdoc/>
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Evaluates this specification's rule against a single in-memory entity.
    /// </summary>
    /// <param name="entity">
    /// The entity to test.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="entity"/> satisfies the rule; otherwise <see langword="false"/>.
    /// </returns>
    public bool IsSatisfiedBy(T entity) => ToExpression().Compile()(entity);

    /// <summary>
    /// Combines this specification with another using logical AND, producing a new specification
    /// that requires both rules to be satisfied.
    /// </summary>
    /// <param name="other">
    /// The specification to combine with this one.
    /// </param>
    /// <returns>
    /// A new specification whose expression is the conjunction of both rules.
    /// </returns>
    public Specification<T> And(Specification<T> other) => new AndSpecification(this, other);

    private sealed class AndSpecification(Specification<T> left, Specification<T> right) : Specification<T>
    {
        public override Expression<Func<T, bool>> ToExpression()
        {
            var parameter = Expression.Parameter(typeof(T));
            var replacer = new ParameterReplacer(parameter);
            var body = Expression.AndAlso(
                replacer.Visit(left.ToExpression().Body)!,
                replacer.Visit(right.ToExpression().Body)!);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }

    private sealed class ParameterReplacer(ParameterExpression parameter) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => parameter;
    }
}
