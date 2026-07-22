using FluentValidation;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Validators;

/// <summary>
/// Field rules for the customer details carried on an order, shared by the create and update
/// commands so both reject the same input for the same reason.
/// </summary>
internal static class OrderCustomerValidationRules
{
    /// <summary>
    /// Requires a non-empty customer name of at most 200 characters, matching the stored column.
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidOrderCustomerName<T>(this IRuleBuilder<T, string> rule)
    {
        return rule.NotEmpty().MaximumLength(200);
    }

    /// <summary>
    /// Requires a non-empty phone number containing 9 to 15 digits.
    /// </summary>
    /// <remarks>
    /// A pre-check that turns the rule into a readable field error.
    /// <see cref="CustomerPhoneNormalizer.Normalize"/> enforces the same rule in the domain and
    /// remains the correctness boundary.
    /// </remarks>
    public static IRuleBuilderOptions<T, string> ValidOrderCustomerPhone<T>(this IRuleBuilder<T, string> rule)
    {
        return rule.NotEmpty()
            .Must(CustomerPhoneNormalizer.HasPersistableDigitCount)
            .WithMessage("Phone must contain 9 to 15 digits.");
    }

    /// <summary>
    /// Allows a missing customer email, and caps a supplied one at the stored column's length.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> ValidOrderCustomerEmail<T>(this IRuleBuilder<T, string?> rule)
    {
        return rule.MaximumLength(254);
    }

    /// <summary>
    /// Allows a missing customer address, and caps a supplied one at the stored column's length.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> ValidOrderCustomerAddress<T>(this IRuleBuilder<T, string?> rule)
    {
        return rule.MaximumLength(500);
    }
}
