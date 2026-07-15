using System.Linq.Expressions;

namespace Sales.Domain.Tests;

public sealed class SpecificationTests
{
    [Theory]
    [InlineData(5, true)]
    [InlineData(15, false)]
    [InlineData(-1, false)]
    public void And_combines_both_predicates(int value, bool expected)
    {
        var spec = new GreaterThanSpecification(0).And(new LessThanSpecification(10));
        Assert.Equal(expected, spec.IsSatisfiedBy(value));
    }

    [Fact]
    public void ToExpression_is_translatable_against_in_memory_source()
    {
        var spec = new GreaterThanSpecification(2).And(new LessThanSpecification(6));
        var result = new[] { 1, 2, 3, 4, 5, 6, 7 }.AsQueryable().Where(spec.ToExpression()).ToArray();
        Assert.Equal([3, 4, 5], result);
    }

    [Fact]
    public void Active_customer_matches_only_customers_that_are_not_deleted()
    {
        var active = Customer.Create("Nguyen Van A", "0901234567");
        var deleted = Customer.Create("Nguyen Van B", "0901234568");
        deleted.Delete("admin");

        var result = new[] { active, deleted }.AsQueryable()
            .Where(new ActiveCustomerSpecification().ToExpression())
            .ToArray();

        Assert.Equal([active], result);
    }

    [Fact]
    public void Active_product_matches_only_enabled_products_that_are_not_deleted()
    {
        var active = Product.Create("sku-1", "Keyboard", 100);
        var disabled = Product.Create("sku-2", "Mouse", 50);
        var deleted = Product.Create("sku-3", "Monitor", 200);
        disabled.Update(disabled.Name, disabled.Price.Amount, false);
        deleted.Delete("admin");

        var result = new[] { active, disabled, deleted }.AsQueryable()
            .Where(new ActiveProductSpecification().ToExpression())
            .ToArray();

        Assert.Equal([active], result);
    }

    private sealed class GreaterThanSpecification(int threshold) : Specification<int>
    {
        public override Expression<Func<int, bool>> ToExpression() => x => x > threshold;
    }

    private sealed class LessThanSpecification(int threshold) : Specification<int>
    {
        public override Expression<Func<int, bool>> ToExpression() => x => x < threshold;
    }
}
