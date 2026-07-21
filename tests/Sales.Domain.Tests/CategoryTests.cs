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

    [Fact]
    public void Category_create_and_update_preserve_description()
    {
        var category = Category.Create("CAT001", "T-Shirt", "  Initial description  ", null, 10);

        Assert.Equal("Initial description", category.Description);

        category.Update("T-Shirt", "  Updated description  ", null, 10);

        Assert.Equal("Updated description", category.Description);
    }

    [Fact]
    public void Category_blank_description_is_normalized_to_null()
    {
        var category = Category.Create("CAT001", "T-Shirt", "Initial description", null, 10);

        category.Update("T-Shirt", "   ", null, 10);

        Assert.Null(category.Description);
    }
}
