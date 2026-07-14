using BuildingBlocks.Contracts;
using Inventory.Application;

namespace Inventory.Tests;

public sealed class ReserveStockCommandValidatorTests
{
    private readonly ReserveStockCommandValidator _validator = new();

    [Fact]
    public void Duplicate_product_lines_are_rejected()
    {
        var productId = Guid.NewGuid();
        var command = new ReserveStockCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            [new OrderLineIntegration(productId, "sku", 1), new OrderLineIntegration(productId, "sku", 2)]);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ReserveStockCommand.Lines));
    }

    [Fact]
    public void Distinct_product_lines_are_valid()
    {
        var command = new ReserveStockCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            [new OrderLineIntegration(Guid.NewGuid(), "sku-1", 1), new OrderLineIntegration(Guid.NewGuid(), "sku-2", 2)]);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
