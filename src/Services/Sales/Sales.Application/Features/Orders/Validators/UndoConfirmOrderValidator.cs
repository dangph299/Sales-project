using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Orders.Commands;

namespace Sales.Application.Features.Orders.Validators;

/// <summary>
/// Validates <see cref="UndoConfirmOrder"/>: identifier and expected version must be present.
/// </summary>
public sealed class UndoConfirmOrderValidator : AbstractValidator<UndoConfirmOrder>
{
    /// <summary>
    /// Configures the validation rules for <see cref="UndoConfirmOrder"/>.
    /// </summary>
    public UndoConfirmOrderValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
        RuleFor(x => x.ExpectedVersion).ValidExpectedVersion();
    }
}
