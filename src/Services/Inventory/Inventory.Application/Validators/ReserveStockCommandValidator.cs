using FluentValidation;

namespace Inventory.Application;

/// <summary>
/// Validates reserve stock commands.
/// </summary>
public sealed class ReserveStockCommandValidator : AbstractValidator<ReserveStockCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ReserveStockCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.OrderVersion).GreaterThan(0);
        RuleFor(x => x.CorrelationId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(x => x.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("A product can only appear once per reservation request.")
            .When(x => x.Lines.Count > 0);
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.ProductId).NotEmpty();
            line.RuleFor(x => x.Sku).NotEmpty();
            line.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}
