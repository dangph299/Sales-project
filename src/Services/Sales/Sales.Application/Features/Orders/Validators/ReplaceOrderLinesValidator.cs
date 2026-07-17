using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Orders.Commands;

namespace Sales.Application.Features.Orders.Validators;

/// <summary>
/// Validates <see cref="ReplaceOrderLines"/>: identifier and expected version must be present, and
/// lines must be non-empty, individually valid, and reference each product at most once.
/// </summary>
public sealed class ReplaceOrderLinesValidator : AbstractValidator<ReplaceOrderLines>
{
    /// <summary>
    /// Configures the validation rules for <see cref="ReplaceOrderLines"/>.
    /// </summary>
    public ReplaceOrderLinesValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
        RuleFor(x => x.ExpectedVersion).ValidExpectedVersion();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new OrderLineInputValidator());
        RuleFor(x => x.Lines).HaveUniqueProducts();
    }
}
