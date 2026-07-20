using Mapster;
using MapsterMapper;
using Sales.Application.Features.Customers.DTOs;
using Sales.Application.Features.Orders.DTOs;
using Sales.Application.Features.Products.DTOs;
using Sales.Domain;

namespace Sales.Application.Tests;

public sealed class MappingTests
{
    private static readonly IMapper Mapper = SalesMapperFactory.Create();

    [Fact]
    public void Mapping_registers_are_scanned_from_the_application_assembly()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(DependencyInjection).Assembly);

        Assert.NotEmpty(config.RuleMap);
    }

    [Fact]
    public void Mapping_configuration_compiles()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(DependencyInjection).Assembly);

        config.Compile();
    }

    [Fact]
    public void Product_mapping_does_not_expose_product_price()
    {
        var product = ProductTestFactory.CreatePublishedProduct("SKU-01", "Keyboard", 125000.4m);

        var dto = Mapper.Map<ProductDto>(product);

        Assert.Null(dto.MinPrice);
        Assert.Null(dto.MaxPrice);
        Assert.Equal("SKU-01-BLK-M", dto.Sku);
        Assert.Equal("Keyboard", dto.Name);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void Product_mapping_carries_soft_delete_state()
    {
        var product = ProductTestFactory.CreatePublishedProduct("SKU-01", "Keyboard", 100m);
        product.Delete("tester");

        var dto = Mapper.Map<ProductDto>(product);

        Assert.True(dto.IsDelete);
        Assert.Equal("tester", dto.DeleteByUser);
        Assert.NotNull(dto.DeletedAt);
    }

    [Fact]
    public void Product_mapping_leaves_delete_fields_null_while_active()
    {
        var product = ProductTestFactory.CreatePublishedProduct("SKU-01", "Keyboard", 100m);

        var dto = Mapper.Map<ProductDto>(product);

        Assert.False(dto.IsDelete);
        Assert.Null(dto.DeleteByUser);
        Assert.Null(dto.DeletedAt);
    }

    [Fact]
    public void Customer_mapping_copies_identity_and_normalized_phone()
    {
        var customer = Customer.Create("Nguyen Van A", "090-123-4567");

        var dto = Mapper.Map<CustomerDto>(customer);

        Assert.Equal(customer.Id, dto.Id);
        Assert.Equal("Nguyen Van A", dto.Name);
        Assert.Equal("0901234567", dto.Phone);
        Assert.Equal(customer.Version, dto.Version);
        Assert.False(dto.IsDelete);
        Assert.Null(dto.DeleteByUser);
    }

    [Fact]
    public void Order_mapping_exposes_status_as_string_and_total_as_amount()
    {
        var order = CreateOrder(quantity: 2, discountPercent: 10m, unitPrice: 125000m);

        var dto = Mapper.Map<OrderDto>(order);

        Assert.Equal("Draft", dto.Status);
        Assert.Equal(225000m, dto.Total);
        Assert.Equal(2, dto.TotalQuantity);
        Assert.Equal(order.CustomerName, dto.CustomerName);
        Assert.Equal(order.CustomerPhone, dto.CustomerPhone);
        Assert.Null(dto.RejectionReason);
    }

    [Fact]
    public void Order_mapping_tracks_status_transitions()
    {
        var order = CreateOrder(quantity: 1, discountPercent: 0m, unitPrice: 100m);
        order.RequestConfirmation();

        Assert.Equal("PendingInventory", Mapper.Map<OrderDto>(order).Status);

        order.RejectInventory("out of stock");
        var rejected = Mapper.Map<OrderDto>(order);

        Assert.Equal("InventoryRejected", rejected.Status);
        Assert.Equal("out of stock", rejected.RejectionReason);
    }

    [Fact]
    public void Order_mapping_exposes_line_discount_and_money_amounts()
    {
        var order = CreateOrder(quantity: 2, discountPercent: 10m, unitPrice: 125000m);

        var line = Assert.Single(Mapper.Map<OrderDto>(order).Lines);

        Assert.Equal(10m, line.DiscountPercent);
        Assert.Equal(125000m, line.UnitPrice);
        Assert.Equal(225000m, line.LineTotal);
        Assert.Equal(2, line.Quantity);
        Assert.Equal("SKU-01-BLK-M", line.Sku);
        Assert.Equal("Keyboard", line.ProductName);
    }

    [Fact]
    public void Order_mapping_projects_every_line()
    {
        var customer = CustomerSnapshot.Create(Guid.NewGuid(), "Nguyen Van A", "0901234567");
        var keyboard = ProductTestFactory.CreatePublishedProduct("SKU-01", "Keyboard", 100m);
        var mouse = ProductTestFactory.CreatePublishedProduct("SKU-02", "Mouse", 50m);
        var order = Order.Create(customer, [
            new(Snapshot(keyboard), 2, 0m),
            new(Snapshot(mouse), 3, 0m)
        ]);

        var dto = Mapper.Map<OrderDto>(order);

        Assert.Equal(2, dto.Lines.Count);
        Assert.Equal(5, dto.TotalQuantity);
        Assert.Equal(350m, dto.Total);
        Assert.Contains(dto.Lines, line => line.Sku == "SKU-01-BLK-M" && line.LineTotal == 200m);
        Assert.Contains(dto.Lines, line => line.Sku == "SKU-02-BLK-M" && line.LineTotal == 150m);
    }

    private static Order CreateOrder(int quantity, decimal discountPercent, decimal unitPrice)
    {
        var customer = CustomerSnapshot.Create(Guid.NewGuid(), "Nguyen Van A", "0901234567");
        var product = ProductTestFactory.CreatePublishedProduct("SKU-01", "Keyboard", unitPrice);
        return Order.Create(customer, [new(Snapshot(product), quantity, discountPercent)]);
    }

    private static ProductSnapshot Snapshot(Product product)
    {
        return ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, product.IsActive);
    }
}
