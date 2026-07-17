using FluentValidation;

namespace Sales.Application.Features.Customers.Validators;

internal static class CustomerValidationRules
{
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

    private static bool HavePlausibleDigitCount(string phone)
    {
        var digitCount = phone.Count(char.IsDigit);
        return digitCount is >= 9 and <= 15;
    }
}
