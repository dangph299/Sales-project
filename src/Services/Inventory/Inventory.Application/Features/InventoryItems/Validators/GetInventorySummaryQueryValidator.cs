using FluentValidation;
using Inventory.Application.Features.InventoryItems.Queries;

namespace Inventory.Application.Features.InventoryItems.Validators;

/// <summary>
/// Validates inventory summary aggregation queries.
/// </summary>
public sealed class GetInventorySummaryQueryValidator : AbstractValidator<GetInventorySummaryQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public GetInventorySummaryQueryValidator()
    {
        RuleFor(x => x.Filter.LowStockThreshold).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1_000_000);
    }
}
