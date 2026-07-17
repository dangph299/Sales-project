using FluentValidation;
using Inventory.Application.Features.InventoryItems.Commands;

namespace Inventory.Application.Features.InventoryItems.Validators;

/// <summary>
/// Validates manual stock adjustment commands.
/// </summary>
public sealed class AdjustInventoryCommandValidator : AbstractValidator<AdjustInventoryCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public AdjustInventoryCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Actor).NotEmpty().MaximumLength(128);
    }
}
