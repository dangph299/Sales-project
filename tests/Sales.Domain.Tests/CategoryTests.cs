using Sales.Domain;

namespace Sales.Domain.Tests;

public sealed class CategoryTests
{
    [Fact]
    public void Category_requires_code()
    {
        Assert.Throws<DomainException>(() => Category.Create("", "T-Shirt", null, null, 10));
    }

    [Fact]
    public void Category_rejects_self_parent()
    {
        var category = Category.Create("CAT001", "T-Shirt", null, null, 10);

        Assert.Throws<DomainException>(() => category.Update("T-Shirt", null, category.Id, 10));
    }
}
