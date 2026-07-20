using FluentValidation;

namespace Sales.Application.Common.Extensions;

internal static class CommonValidationRules
{
    /// <summary>
    /// Requires the rule's <see cref="Guid"/> value to be non-empty, as expected for an aggregate identifier.
    /// </summary>
    public static IRuleBuilderOptions<T, Guid> ValidAggregateId<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty();

    /// <summary>
    /// Requires the rule's version value to be non-negative, as expected for an optimistic
    /// concurrency check.
    /// </summary>
    public static IRuleBuilderOptions<T, long> ValidExpectedVersion<T>(this IRuleBuilder<T, long> rule) =>
        rule.GreaterThanOrEqualTo(0);
}
