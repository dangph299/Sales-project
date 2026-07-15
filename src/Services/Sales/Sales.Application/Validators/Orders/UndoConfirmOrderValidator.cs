using FluentValidation;

namespace Sales.Application;

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
