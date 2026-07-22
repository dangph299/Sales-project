using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure.Tests;

public sealed class ReplaceOrderLinesPersistenceTests
{
    [Fact]
    public async Task Adding_order_line_to_existing_order_persists_without_concurrency_conflict()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();

        var firstProduct = ProductTestFactory.CreatePublishedProduct("PRD-LINE-001", "First", 100_000);
        var secondProduct = ProductTestFactory.CreatePublishedProduct("PRD-LINE-002", "Second", 200_000);
        var firstVariant = ProductTestFactory.PrimaryVariant(firstProduct);
        var secondVariant = ProductTestFactory.PrimaryVariant(secondProduct);
        var firstSnapshot = ProductSnapshot.Create(
            firstProduct.Id,
            firstVariant.Id,
            firstProduct.ProductCode,
            firstProduct.Name,
            firstVariant.Sku,
            "BLK",
            "Black",
            "M",
            firstVariant.Price,
            isActive: true,
            isSellThroughDiscontinued: false);
        var secondSnapshot = ProductSnapshot.Create(
            secondProduct.Id,
            secondVariant.Id,
            secondProduct.ProductCode,
            secondProduct.Name,
            secondVariant.Sku,
            "BLK",
            "Black",
            "M",
            secondVariant.Price,
            isActive: true,
            isSellThroughDiscontinued: false);
        var customer = OrderCustomerSnapshot.Create(Guid.NewGuid(), "Nguyen Van A", "0901234567", null, null);
        var order = Order.Create(
            OrderTestFactory.NextOrderCode(),
            customer,
            [new OrderLineItem(firstSnapshot, 1, 0m)]);

        await using (var seed = fixture.CreateContext())
        {
            seed.Orders.Add(order);
            await seed.SaveChangesAsync();
        }

        await using (var update = fixture.CreateContext())
        {
            var persisted = await update.Orders.Include(x => x.Lines).SingleAsync(x => x.Id == order.Id);

            persisted.ReplaceLines(
            [
                new OrderLineItem(firstSnapshot, 1, 0m),
                new OrderLineItem(secondSnapshot, 2, 0m)
            ]);

            await update.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();
        var updated = await verify.Orders.Include(x => x.Lines).SingleAsync(x => x.Id == order.Id);
        Assert.Equal(2, updated.Version);
        Assert.Equal(2, updated.Lines.Count);
    }
}
