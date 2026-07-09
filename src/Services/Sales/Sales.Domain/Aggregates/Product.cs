namespace Sales.Domain;

/// <summary>
/// Aggregate root for a catalog product. Owns invariants around SKU/name/price validity and raises
/// the domain events consumed for auditing.
/// </summary>
public sealed class Product : AggregateRoot
{
    private Product() { }
    private Product(Guid id, string sku, string name, Money price)
    {
        Id = id;
        Sku = NormalizeSku(sku);
        Rename(name);
        Price = price;
        IsActive = true;
    }

    /// <summary>
    /// Gets the product's normalized (trimmed, upper-invariant) SKU.
    /// </summary>
    public string Sku { get; private set; } = null!;

    /// <summary>
    /// Gets the product's name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the product's unit price.
    /// </summary>
    public Money Price { get; private set; }

    /// <summary>
    /// Gets whether the product can currently be ordered.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Creates a new <see cref="Product"/> aggregate, active by default, and raises
    /// <see cref="ProductCreatedDomainEvent"/>.
    /// </summary>
    /// <param name="sku">
    /// The product's SKU.
    /// </param>
    /// <param name="name">
    /// The product's name.
    /// </param>
    /// <param name="price">
    /// The product's unit price in VND.
    /// </param>
    /// <returns>
    /// The newly created product.
    /// </returns>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="sku"/>/<paramref name="name"/> is empty/whitespace or
    /// <paramref name="price"/> is negative.
    /// </exception>
    public static Product Create(string sku, string name, decimal price)
    {
        var product = new Product(Guid.NewGuid(), sku, name, Money.Vnd(price));
        product.Raise(new ProductCreatedDomainEvent(product.Id, product.Sku, product.Name, product.Price.Amount));
        return product;
    }

    /// <summary>
    /// Updates the product's name, price, and active flag. Raises <see cref="ProductUpdatedDomainEvent"/>
    /// and increments <see cref="AggregateRoot.Version"/> only if a value actually changed.
    /// </summary>
    /// <param name="name">
    /// The product's new name.
    /// </param>
    /// <param name="price">
    /// The product's new unit price in VND.
    /// </param>
    /// <param name="isActive">
    /// Whether the product should be active after the update.
    /// </param>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="name"/> is empty/whitespace or <paramref name="price"/> is negative.
    /// </exception>
    public void Update(string name, decimal price, bool isActive)
    {
        var oldName = Name;
        var oldPrice = Price.Amount;
        var oldIsActive = IsActive;
        Rename(name);
        Price = Money.Vnd(price);
        IsActive = isActive;
        if (oldName == Name && oldPrice == Price.Amount && oldIsActive == IsActive) return;
        Touch();
        Raise(new ProductUpdatedDomainEvent(Id, oldName, oldPrice, oldIsActive, Name, Price.Amount, IsActive));
    }

    private void Rename(string name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new DomainException("Product name is required.") : name.Trim();
    }

    private static string NormalizeSku(string sku) =>
        string.IsNullOrWhiteSpace(sku) ? throw new DomainException("SKU is required.") : sku.Trim().ToUpperInvariant();
}
