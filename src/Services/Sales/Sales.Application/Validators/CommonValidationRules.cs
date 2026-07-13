using FluentValidation;

namespace Sales.Application;

internal static class CommonValidationRules
{
    /// <summary>
    /// Requires the rule's <see cref="Guid"/> value to be non-empty, as expected for an aggregate identifier.
    /// </summary>
    /// <param name="rule">Rule builder to extend.</param>
    /// <returns>Rule builder, to allow chaining further rules.</returns>
    public static IRuleBuilderOptions<T, Guid> ValidAggregateId<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty();

    /// <summary>
    /// Requires the rule's version value to be non-negative, as expected for an optimistic
    /// concurrency check.
    /// </summary>
    /// <param name="rule">Rule builder to extend.</param>
    /// <returns>Rule builder, to allow chaining further rules.</returns>
    public static IRuleBuilderOptions<T, long> ValidExpectedVersion<T>(this IRuleBuilder<T, long> rule) =>
        rule.GreaterThanOrEqualTo(0);

    /// <summary>
    /// Requires the rule's string value to be non-empty and at most 200 characters, as expected for
    /// a customer name.
    /// </summary>
    /// <param name="rule">Rule builder to extend.</param>
    /// <returns>Rule builder, to allow chaining further rules.</returns>
    public static IRuleBuilderOptions<T, string> ValidCustomerName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(200);

    /// <summary>
    /// Requires the rule's string value to be non-empty and contain 9 to 15 digits, as expected for
    /// a phone number.
    /// </summary>
    /// <param name="rule">Rule builder to extend.</param>
    /// <returns>Rule builder, to allow chaining further rules.</returns>
    public static IRuleBuilderOptions<T, string> ValidPhone<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Must(HavePlausibleDigitCount)
            .WithMessage("Phone must contain 9 to 15 digits.");

    /// <summary>
    /// Requires that no product identifier occurs more than once across the rule's order lines.
    /// </summary>
    /// <param name="rule">Rule builder to extend.</param>
    /// <returns>Rule builder, to allow chaining further rules.</returns>
    public static IRuleBuilderOptions<T, IReadOnlyCollection<OrderLineInput>> HaveUniqueProducts<T>(
        this IRuleBuilder<T, IReadOnlyCollection<OrderLineInput>> rule) =>
        rule.Must(lines => lines.Count == 0 || lines.Select(x => x.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("A product can occur only once in an order.");

    private static bool HavePlausibleDigitCount(string phone)
    {
        var digitCount = phone.Count(char.IsDigit);
        return digitCount is >= 9 and <= 15;
    }
}
