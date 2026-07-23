using Inventory.Application.Features.InventoryItems.Queries;
using Inventory.Application.Features.InventoryItems.Validators;

namespace Inventory.Tests;

public sealed class GetInventoryByProductVariantsQueryValidatorTests
{
    [Fact]
    public void Empty_list_is_valid()
    {
        var validator = new GetInventoryByProductVariantsQueryValidator();

        var result = validator.Validate(new GetInventoryByProductVariantsQuery([]));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Too_many_ids_is_invalid()
    {
        var ids = Enumerable.Range(0, GetInventoryByProductVariantsQueryValidator.MaxProductVariantIds + 1)
            .Select(_ => Guid.NewGuid())
            .ToArray();
        var validator = new GetInventoryByProductVariantsQueryValidator();

        var result = validator.Validate(new GetInventoryByProductVariantsQuery(ids));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Empty_id_is_invalid()
    {
        var validator = new GetInventoryByProductVariantsQueryValidator();

        var result = validator.Validate(new GetInventoryByProductVariantsQuery([Guid.Empty]));

        Assert.False(result.IsValid);
    }
}
