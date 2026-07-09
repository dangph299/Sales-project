using Sales.Application;
using Sales.Domain;

namespace Sales.Application.Tests;

public sealed class MappingTests
{
    [Fact]
    public void Product_mapping_exposes_vnd_amount()
    {
        var product = Product.Create("SKU-01", "Keyboard", 125000.4m);
        var dto = product.ToDto();
        Assert.Equal(125000m, dto.Price);
        Assert.Equal("SKU-01", dto.Sku);
    }
}
