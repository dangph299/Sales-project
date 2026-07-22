namespace Sales.Domain;

/// <summary>
/// An immutable, validated snapshot of a customer's identifying data at a point in time, used
/// wherever an order needs to record who it was placed for without holding a live reference.
/// </summary>
public sealed record CustomerSnapshot
{
    /// <summary>
    /// Gets the unique identifier of the customer this snapshot was taken from.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the customer's name at the time the snapshot was taken.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the customer's normalized phone number at the time the snapshot was taken.
    /// </summary>
    public string Phone { get; }

    private CustomerSnapshot(Guid id, string name, string phone) => (Id, Name, Phone) = (id, name, phone);

    /// <summary>
    /// Creates a validated <see cref="CustomerSnapshot"/>.
    /// </summary>
    /// <param name="id">Customer identifier.</param>
    /// <param name="name">Customer's name.</param>
    /// <param name="phone">Customer's phone number, normalized via <see cref="CustomerPhoneNormalizer"/>.</param>
    /// <returns>Validated snapshot.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="id"/> is empty or <paramref name="name"/> is empty/whitespace.</exception>
    public static CustomerSnapshot Create(Guid id, string name, string phone)
    {
        if (id == Guid.Empty) throw new DomainException("Customer id is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Customer name is required.");
        return new(id, name.Trim(), CustomerPhoneNormalizer.Normalize(phone));
    }
}
