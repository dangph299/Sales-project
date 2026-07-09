using System.Linq.Expressions;
using Sales.Domain;

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

    private sealed class GreaterThanSpecification(int threshold) : Specification<int>
    {
        public override Expression<Func<int, bool>> ToExpression() => x => x > threshold;
    }

    private sealed class LessThanSpecification(int threshold) : Specification<int>
    {
        public override Expression<Func<int, bool>> ToExpression() => x => x < threshold;
    }
}
