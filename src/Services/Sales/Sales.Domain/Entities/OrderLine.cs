namespace Sales.Domain;

/// <summary>
/// A single product line owned by an <see cref="Order"/> aggregate. Not an aggregate root itself —
/// it is only ever created, replaced, and persisted through its owning <see cref="Order"/>.
/// </summary>
public sealed class OrderLine : Entity<Guid>
{
    private OrderLine() { }
    private OrderLine(Guid orderId, ProductSnapshot product, int quantity, decimal discountPercent)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = product.Id;
        ProductVariantId = product.ProductVariantId;
        ProductCode = product.ProductCode;
        Sku = product.Sku;
        ProductName = product.Name;
        ColorCode = product.ColorCode;
        ColorName = product.ColorName;
        SizeCode = product.SizeCode;
        UnitPrice = product.UnitPrice;
        Quantity = quantity;
        DiscountPercent = discountPercent;
    }

    /// <summary>
    /// Gets the unique identifier of the <see cref="Order"/> that owns this line.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the product this line refers to.
    /// </summary>
    public Guid ProductId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public string ProductCode { get; private set; } = null!;

    /// <summary>
    /// Gets the product's SKU as it was at the time this line was created or last replaced.
    /// </summary>
    public string Sku { get; private set; } = null!;

    /// <summary>
    /// Gets the product's name as it was at the time this line was created or last replaced.
    /// </summary>
    public string ProductName { get; private set; } = null!;

    public string ColorCode { get; private set; } = null!;

    public string ColorName { get; private set; } = null!;

    public string SizeCode { get; private set; } = null!;

    /// <summary>
    /// Gets the product's unit price as it was at the time this line was created or last replaced.
    /// </summary>
    public Money UnitPrice { get; private set; }

    /// <summary>
    /// Gets the quantity requested for this line.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Gets the discount percentage (0-100) applied to this line.
    /// </summary>
    public decimal DiscountPercent { get; private set; }

    /// <summary>
    /// Gets the total monetary amount for this line, after applying the discount to the unit price
    /// and quantity.
    /// </summary>
    public Money LineTotal => Money.Vnd(UnitPrice.Amount * Quantity * (1 - DiscountPercent / 100m));

    internal static OrderLine Create(Guid orderId, ProductSnapshot product, int quantity, decimal discountPercent)
    {
        Validate(quantity, discountPercent);
        return new OrderLine(orderId, product, quantity, discountPercent);
    }

    internal void ReplaceWith(ProductSnapshot product, int quantity, decimal discountPercent)
    {
        Validate(quantity, discountPercent);
        ProductId = product.Id;
        ProductVariantId = product.ProductVariantId;
        ProductCode = product.ProductCode;
        Sku = product.Sku;
        ProductName = product.Name;
        ColorCode = product.ColorCode;
        ColorName = product.ColorName;
        SizeCode = product.SizeCode;
        UnitPrice = product.UnitPrice;
        Quantity = quantity;
        DiscountPercent = discountPercent;
    }

    private static void Validate(int quantity, decimal discountPercent)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        if (discountPercent is < 0 or > 100) throw new DomainException("Discount must be between 0 and 100.");
    }
}
