namespace Inventory.Domain;

/// <summary>
/// Tracks available and reserved stock for a single product. Owns the invariant that available
/// stock can never go negative.
/// </summary>
public sealed class InventoryItem
{
    private InventoryItem() { }

    /// <summary>
    /// Gets the unique identifier of the product this item tracks stock for.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Gets the product's normalized SKU.
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
    /// Gets the optimistic concurrency version of this item.
    /// </summary>
    public long Version { get; private set; } = 1;

    /// <summary>
    /// Creates a new inventory item with an initial available quantity.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the product.
    /// </param>
    /// <param name="sku">
    /// The product's SKU.
    /// </param>
    /// <param name="available">
    /// The initial available quantity. Must not be negative.
    /// </param>
    /// <returns>
    /// The newly created inventory item.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="available"/> is negative.
    /// </exception>
    public static InventoryItem Create(Guid id, string sku, int available)
    {
        if (available < 0) throw new InvalidOperationException("Initial stock cannot be negative.");
        return new() { ProductId = id, Sku = sku.Trim().ToUpperInvariant(), Available = available };
    }

    /// <summary>
    /// Adjusts the available quantity by a signed delta, for manual stock corrections.
    /// </summary>
    /// <param name="delta">
    /// The signed quantity to add to <see cref="Available"/>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when applying <paramref name="delta"/> would make <see cref="Available"/> negative.
    /// </exception>
    public void Adjust(int delta)
    {
        if (Available + delta < 0) throw new InvalidOperationException("Available stock cannot become negative.");
        Available += delta;
        Version++;
    }

    /// <summary>
    /// Moves a quantity from <see cref="Available"/> to <see cref="Reserved"/>.
    /// </summary>
    /// <param name="quantity">
    /// The quantity to reserve. Must be positive and no more than <see cref="Available"/>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="quantity"/> is not positive or exceeds <see cref="Available"/>.
    /// </exception>
    public void Reserve(int quantity)
    {
        if (quantity <= 0 || Available < quantity) throw new InvalidOperationException($"Insufficient stock for {Sku}.");
        Available -= quantity;
        Reserved += quantity;
        Version++;
    }

    /// <summary>
    /// Moves a quantity from <see cref="Reserved"/> back to <see cref="Available"/>.
    /// </summary>
    /// <param name="quantity">
    /// The quantity to release. Must be positive and no more than <see cref="Reserved"/>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="quantity"/> is not positive or exceeds <see cref="Reserved"/>.
    /// </exception>
    public void Release(int quantity)
    {
        if (quantity <= 0 || Reserved < quantity) throw new InvalidOperationException("Invalid reservation release.");
        Reserved -= quantity;
        Available += quantity;
        Version++;
    }
}
