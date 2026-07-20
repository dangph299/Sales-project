using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Orders.DTOs;

namespace Sales.Application.Features.Orders.Validators;

/// <summary>
/// Validates a single <see cref="OrderLineInput"/>: product variant identifier must be present, quantity
/// must be positive, and discount must be between 0 and 100.
/// </summary>
public sealed class OrderLineInputValidator : AbstractValidator<OrderLineInput>
{
    /// <summary>
    /// Configures the validation rules for <see cref="OrderLineInput"/>.
    /// </summary>
    public OrderLineInputValidator()
    {
        RuleFor(x => x.ProductVariantId).ValidAggregateId();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.DiscountPercent).NotNull().InclusiveBetween(0, 100);
    }
}
