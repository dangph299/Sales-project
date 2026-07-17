using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Orders.Commands;

namespace Sales.Application.Features.Orders.Validators;

/// <summary>
/// Validates <see cref="CancelOrder"/>: identifier and expected version must be present.
/// </summary>
public sealed class CancelOrderValidator : AbstractValidator<CancelOrder>
{
    /// <summary>
    /// Configures the validation rules for <see cref="CancelOrder"/>.
    /// </summary>
    public CancelOrderValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
        RuleFor(x => x.ExpectedVersion).ValidExpectedVersion();
    }
}
