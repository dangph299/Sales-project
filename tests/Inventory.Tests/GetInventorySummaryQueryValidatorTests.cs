using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.InventoryItems.Queries;
using Inventory.Application.Features.InventoryItems.Validators;

namespace Inventory.Tests;

public sealed class GetInventorySummaryQueryValidatorTests
{
    private readonly GetInventorySummaryQueryValidator _validator = new();

    [Fact]
    public void Negative_threshold_is_rejected()
    {
        var query = new GetInventorySummaryQuery(new InventorySummaryFilter(-1));

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Threshold_of_zero_is_valid()
    {
        var query = new GetInventorySummaryQuery(new InventorySummaryFilter(0));

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Threshold_of_five_is_valid()
    {
        var query = new GetInventorySummaryQuery(new InventorySummaryFilter(5));

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }
}
