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

    public Guid ProductVariantId { get; }

    public string ProductCode { get; }

    /// <summary>
    /// Gets the product's SKU at the time the snapshot was taken.
    /// </summary>
    public string Sku { get; }

    /// <summary>
    /// Gets the product's name at the time the snapshot was taken.
    /// </summary>
    public string Name { get; }

    public string ColorCode { get; }

    public string ColorName { get; }

    public string SizeCode { get; }

    public bool IsSellThroughDiscontinued { get; }

    /// <summary>
    /// Gets the product's unit price at the time the snapshot was taken.
    /// </summary>
    public Money UnitPrice { get; }

    private ProductSnapshot(
        Guid id,
        Guid productVariantId,
        string productCode,
        string sku,
        string name,
        string colorCode,
        string colorName,
        string sizeCode,
        Money unitPrice,
        bool isSellThroughDiscontinued)
    {
        Id = id;
        ProductVariantId = productVariantId;
        ProductCode = productCode;
        Sku = sku;
        Name = name;
        ColorCode = colorCode;
        ColorName = colorName;
        SizeCode = sizeCode;
        UnitPrice = unitPrice;
        IsSellThroughDiscontinued = isSellThroughDiscontinued;
    }

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
        if (!isActive) throw new DomainException("Only sellable products can be ordered.");
        if (id == Guid.Empty) throw new DomainException("Product id is required.");
        if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name)) throw new DomainException("Product snapshot is incomplete.");
        return new(id, id, sku.Trim().ToUpperInvariant(), sku.Trim().ToUpperInvariant(), name.Trim(), string.Empty, string.Empty, string.Empty, unitPrice, false);
    }

    public static ProductSnapshot Create(
        Guid productId,
        Guid productVariantId,
        string productCode,
        string productName,
        string sku,
        string colorCode,
        string colorName,
        string sizeCode,
        Money unitPrice,
        bool isActive,
        bool isSellThroughDiscontinued = false)
    {
        if (!isActive) throw new DomainException("Only sellable product variants can be ordered.");
        if (productId == Guid.Empty) throw new DomainException("Product id is required.");
        if (productVariantId == Guid.Empty) throw new DomainException("Product variant id is required.");
        if (string.IsNullOrWhiteSpace(productCode) ||
            string.IsNullOrWhiteSpace(productName) ||
            string.IsNullOrWhiteSpace(sku) ||
            string.IsNullOrWhiteSpace(colorCode) ||
            string.IsNullOrWhiteSpace(colorName) ||
            string.IsNullOrWhiteSpace(sizeCode))
        {
            throw new DomainException("Product snapshot is incomplete.");
        }

        return new(
            productId,
            productVariantId,
            productCode.Trim().ToUpperInvariant(),
            sku.Trim().ToUpperInvariant(),
            productName.Trim(),
            colorCode.Trim().ToUpperInvariant(),
            colorName.Trim(),
            sizeCode.Trim().ToUpperInvariant(),
            unitPrice,
            isSellThroughDiscontinued);
    }
}
