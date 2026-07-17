using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Orders.Commands;

namespace Sales.Application.Features.Orders.Validators;

/// <summary>
/// Validates <see cref="ConfirmOrder"/>: identifier and expected version must be present.
/// </summary>
public sealed class ConfirmOrderValidator : AbstractValidator<ConfirmOrder>
{
    /// <summary>
    /// Configures the validation rules for <see cref="ConfirmOrder"/>.
    /// </summary>
    public ConfirmOrderValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
        RuleFor(x => x.ExpectedVersion).ValidExpectedVersion();
    }
}
