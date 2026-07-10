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

    [Fact]
    public void Order_mapping_exposes_line_discount()
    {
        var customer = CustomerSnapshot.Create(Guid.NewGuid(), "Nguyen Van A", "0901234567");
        var product = Product.Create("SKU-01", "Keyboard", 125000m);
        var snapshot = ProductSnapshot.Create(product.Id, product.Sku, product.Name, product.Price, product.IsActive);

        var order = Order.Create(customer, [new(snapshot, 2, 10m)]);

        var line = Assert.Single(order.ToDto().Lines);
        Assert.Equal(10m, line.DiscountPercent);
        Assert.Equal(225000m, line.LineTotal);
    }
}
