using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Orders.Commands;

namespace Sales.Application.Features.Orders.Validators;

/// <summary>
/// Validates <see cref="CreateOrder"/>: customer identifier must be present, and lines must be
/// non-empty, individually valid, and reference each product at most once.
/// </summary>
public sealed class CreateOrderValidator : AbstractValidator<CreateOrder>
{
    /// <summary>
    /// Configures the validation rules for <see cref="CreateOrder"/>.
    /// </summary>
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).ValidAggregateId();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new OrderLineInputValidator());
        RuleFor(x => x.Lines).HaveUniqueProducts();
    }
}
