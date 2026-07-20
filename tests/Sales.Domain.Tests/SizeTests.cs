using Sales.Domain;

namespace Sales.Domain.Tests;

public sealed class SizeTests
{
    [Fact]
    public void Sizes_sort_in_display_order()
    {
        var sizes = new[]
        {
            Size.Create(Guid.NewGuid(), "XL", "Extra Large", 60),
            Size.Create(Guid.NewGuid(), "M", "Medium", 40),
            Size.Create(Guid.NewGuid(), "S", "Small", 30)
        };

        var orderedCodes = sizes.OrderBy(x => x.SortOrder).Select(x => x.Code).ToArray();

        Assert.Equal(["S", "M", "XL"], orderedCodes);
    }
}
