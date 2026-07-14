using FluentValidation;

namespace Inventory.Application;

/// <summary>
/// Validates release stock commands.
/// </summary>
public sealed class ReleaseStockCommandValidator : AbstractValidator<ReleaseStockCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ReleaseStockCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.OrderVersion).GreaterThan(0);
        RuleFor(x => x.CorrelationId).NotEmpty();
    }
}
