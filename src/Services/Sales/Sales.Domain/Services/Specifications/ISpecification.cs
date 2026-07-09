using System.Linq.Expressions;

namespace Sales.Domain;

/// <summary>
/// A composable query rule for <typeparamref name="T"/>, expressed as an <see cref="Expression"/> so
/// it can be translated by an ORM instead of only evaluated in memory.
/// </summary>
/// <typeparam name="T">
/// The type the specification's rule applies to.
/// </typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Builds the predicate expression that represents this specification's rule.
    /// </summary>
    /// <returns>
    /// An expression tree evaluating to <see langword="true"/> for entities that satisfy the rule.
    /// </returns>
    Expression<Func<T, bool>> ToExpression();
}
