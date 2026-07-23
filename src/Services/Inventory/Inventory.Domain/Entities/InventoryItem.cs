namespace Inventory.Domain;

/// <summary>
/// Tracks available and reserved stock for a single product variant. Owns the invariant that
/// available stock can never go negative.
/// </summary>
public sealed class InventoryItem : IEntity<Guid>
{
    private InventoryItem() { }

    /// <summary>
    /// Gets the unique identifier of the product variant this item tracks stock for.
    /// </summary>
    public Guid ProductVariantId { get; private set; }

    Guid IEntity<Guid>.Id => ProductVariantId;

    /// <summary>
    /// Gets the product variant's normalized SKU.
    /// </summary>
    public string Sku { get; private set; } = null!;

    /// <summary>
    /// Gets the quantity currently available to reserve.
    /// </summary>
    public int Available { get; private set; }

    /// <summary>
    /// Gets the quantity currently reserved against active reservations.
    /// </summary>
    public int Reserved { get; private set; }

    /// <summary>
    /// Gets the UTC instant this inventory row was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC instant this inventory row was last changed.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the optimistic concurrency version of this item.
    /// </summary>
    public long Version { get; private set; } = 1;

    /// <summary>
    /// Creates a new inventory item with an initial available quantity.
    /// </summary>
    /// <param name="productVariantId">Product variant identifier.</param>
    /// <param name="sku">Product variant's SKU.</param>
    /// <param name="available">Initial available quantity. Must not be negative.</param>
    /// <returns>Newly created inventory item.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="available"/> is negative.</exception>
    public static InventoryItem Create(Guid productVariantId, string sku, int available)
    {
        if (available < 0) throw new InvalidOperationException("Initial stock cannot be negative.");
        var now = DateTimeOffset.UtcNow;
        return new() { ProductVariantId = productVariantId, Sku = sku.Trim().ToUpperInvariant(), Available = available, CreatedAt = now, UpdatedAt = now };
    }

    /// <summary>
    /// Adjusts the available quantity by a signed delta, for manual stock corrections.
    /// </summary>
    /// <param name="delta">Signed quantity to add to <see cref="Available"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when applying <paramref name="delta"/> would make <see cref="Available"/> negative.</exception>
    public void Adjust(int delta)
    {
        if (Available + delta < 0) throw new InvalidOperationException("Available stock cannot become negative.");
        if (delta == 0) return;
        Available += delta;
        Touch();
    }

    /// <summary>
    /// Moves a quantity from <see cref="Available"/> to <see cref="Reserved"/>.
    /// </summary>
    /// <param name="quantity">Quantity to reserve. Must be positive and no more than <see cref="Available"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="quantity"/> is not positive or exceeds <see cref="Available"/>.</exception>
    public void Reserve(int quantity)
    {
        if (quantity <= 0 || Available < quantity) throw new InvalidOperationException($"Insufficient stock for {Sku}.");
        Available -= quantity;
        Reserved += quantity;
        Touch();
    }

    /// <summary>
    /// Moves a quantity from <see cref="Reserved"/> back to <see cref="Available"/>.
    /// </summary>
    /// <param name="quantity">Quantity to release. Must be positive and no more than <see cref="Reserved"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="quantity"/> is not positive or exceeds <see cref="Reserved"/>.</exception>
    public void Release(int quantity)
    {
        if (quantity <= 0 || Reserved < quantity) throw new InvalidOperationException("Invalid reservation release.");
        Reserved -= quantity;
        Available += quantity;
        Touch();
    }

    private void Touch()
    {
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
