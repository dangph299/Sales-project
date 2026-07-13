namespace Sales.Domain;

/// <summary>
/// An immutable, validated snapshot of a product's identifying data and price at a point in time,
/// used to record what an order line referred to without holding a live reference to the product.
/// </summary>
public sealed record ProductSnapshot
{
    /// <summary>
    /// Gets the unique identifier of the product this snapshot was taken from.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the product's SKU at the time the snapshot was taken.
    /// </summary>
    public string Sku { get; }

    /// <summary>
    /// Gets the product's name at the time the snapshot was taken.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the product's unit price at the time the snapshot was taken.
    /// </summary>
    public Money UnitPrice { get; }

    private ProductSnapshot(Guid id, string sku, string name, Money unitPrice) =>
        (Id, Sku, Name, UnitPrice) = (id, sku, name, unitPrice);

    /// <summary>
    /// Creates a validated <see cref="ProductSnapshot"/> for an active product.
    /// </summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="sku">Product's SKU.</param>
    /// <param name="name">Product's name.</param>
    /// <param name="unitPrice">Product's unit price.</param>
    /// <param name="isActive">Whether the product is currently active. Must be <see langword="true"/>.</param>
    /// <returns>Validated snapshot.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="isActive"/> is <see langword="false"/>, <paramref name="id"/> is empty, or <paramref name="sku"/>/<paramref name="name"/> is empty/whitespace.</exception>
    public static ProductSnapshot Create(Guid id, string sku, string name, Money unitPrice, bool isActive)
    {
        if (!isActive) throw new DomainException("Inactive products cannot be ordered.");
        if (id == Guid.Empty) throw new DomainException("Product id is required.");
        if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name)) throw new DomainException("Product snapshot is incomplete.");
        return new(id, sku.Trim().ToUpperInvariant(), name.Trim(), unitPrice);
    }
}
