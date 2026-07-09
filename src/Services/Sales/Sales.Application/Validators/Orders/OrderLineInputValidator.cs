using FluentValidation;

namespace Sales.Application;

/// <summary>
/// Validates a single <see cref="OrderLineInput"/>: product identifier must be present, quantity
/// must be positive, and discount must be between 0 and 100.
/// </summary>
public sealed class OrderLineInputValidator : AbstractValidator<OrderLineInput>
{
    /// <summary>
    /// Configures the validation rules for <see cref="OrderLineInput"/>.
    /// </summary>
    public OrderLineInputValidator()
    {
        RuleFor(x => x.ProductId).ValidAggregateId();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}
