using FluentValidation;
using Inventory.Application.Features.InventoryItems.Queries;

namespace Inventory.Application.Features.InventoryItems.Validators;

/// <summary>
/// Validates batch inventory read requests.
/// </summary>
public sealed class GetInventoryByProductVariantsQueryValidator : AbstractValidator<GetInventoryByProductVariantsQuery>
{
    /// <summary>Maximum product variant ids accepted by the batch endpoint.</summary>
    public const int MaxProductVariantIds = 100;

    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public GetInventoryByProductVariantsQueryValidator()
    {
        RuleFor(x => x.ProductVariantIds)
            .NotNull()
            .Must(ids => ids is null || ids.Count <= MaxProductVariantIds)
            .WithMessage($"At most {MaxProductVariantIds} product variant ids can be requested.");

        RuleForEach(x => x.ProductVariantIds)
            .NotEmpty();
    }
}
