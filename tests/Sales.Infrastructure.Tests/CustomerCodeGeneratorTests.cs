using Sales.Domain;

namespace Sales.Infrastructure.Tests;

public sealed class CustomerCodeGeneratorTests
{
    [Fact]
    public async Task NextCode_includes_soft_deleted_customers_when_calculating_the_next_sequence()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var active = Customer.Create("CUS000001", "Active Customer", "0901234567");
        var deleted = Customer.Create("CUS000002", "Deleted Customer", "0901234568");
        deleted.Delete("test");
        await fixture.SeedAsync(active, deleted);

        await using var context = fixture.CreateContext();
        var generator = new CustomerCodeGenerator(context);

        var nextCode = await generator.NextCodeAsync();

        Assert.Equal("CUS000003", nextCode);
    }
}
