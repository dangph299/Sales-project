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
        return rule.NotEmpty()
            .MaximumLength(200)
            .WithMessage("Customer name must be at most 200 characters.");
    }

    /// <summary>
    /// Requires a non-empty phone number of at most 32 characters containing 9 to 15 digits.
    /// </summary>
    /// <remarks>
    /// A pre-check that turns the rule into a readable field error.
    /// <see cref="CustomerPhoneNormalizer.Normalize"/> enforces the digit rule in the domain and
    /// remains the correctness boundary. The 32-character cap matches the stored column
    /// (see OrderConfiguration): a value can carry a valid digit count yet still be too long to
    /// persist once its formatting is included, and without this rule that reaches the database as a
    /// 500 instead of a 400.
    /// </remarks>
    public static IRuleBuilderOptions<T, string> ValidOrderCustomerPhone<T>(this IRuleBuilder<T, string> rule)
    {
        return rule.NotEmpty()
            .MaximumLength(32)
            .WithMessage("Phone must be at most 32 characters.")
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
