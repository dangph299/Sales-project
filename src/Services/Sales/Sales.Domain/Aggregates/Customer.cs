namespace Sales.Domain;

/// <summary>
/// Aggregate root for a customer. Owns invariants around name/phone validity and raises the
/// domain events consumed for auditing.
/// </summary>
public sealed class Customer : AggregateRoot
{
    private Customer() { }
    private Customer(Guid id, string name, string phone)
    {
        Id = id;
        SetDetails(name, phone);
    }

    /// <summary>
    /// Gets the customer's name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the customer's normalized phone number (digits only).
    /// </summary>
    public string Phone { get; private set; } = null!;

    /// <summary>
    /// Gets <see cref="Phone"/> with its digits reversed, used to support efficient suffix search
    /// (<c>LIKE</c> on a reversed column matches a phone-number suffix as a prefix).
    /// </summary>
    public string ReversedPhone { get; private set; } = null!;

    /// <summary>
    /// Gets whether the customer has been soft-deleted.
    /// </summary>
    public bool IsDelete { get; private set; }

    /// <summary>
    /// Gets the user that soft-deleted this customer, or <see langword="null"/> if it is active.
    /// </summary>
    public string? DeleteByUser { get; private set; }

    /// <summary>
    /// Gets the UTC instant this customer was soft-deleted, or <see langword="null"/> if it is active.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    /// Creates a new <see cref="Customer"/> aggregate and raises <see cref="CustomerCreatedDomainEvent"/>.
    /// </summary>
    /// <param name="name">
    /// The customer's name.
    /// </param>
    /// <param name="phone">
    /// The customer's phone number, in any format containing 9 to 15 digits.
    /// </param>
    /// <returns>
    /// The newly created customer.
    /// </returns>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="name"/> is empty/whitespace or <paramref name="phone"/> does not
    /// contain 9 to 15 digits.
    /// </exception>
    public static Customer Create(string name, string phone)
    {
        var customer = new Customer(Guid.NewGuid(), name, phone);
        customer.Raise(new CustomerCreatedDomainEvent(customer.Id, customer.Name, customer.Phone));
        return customer;
    }

    /// <summary>
    /// Updates the customer's name and phone number. Raises <see cref="CustomerUpdatedDomainEvent"/>
    /// and increments <see cref="AggregateRoot.Version"/> only if a value actually changed.
    /// </summary>
    /// <param name="name">
    /// The customer's new name.
    /// </param>
    /// <param name="phone">
    /// The customer's new phone number, in any format containing 9 to 15 digits.
    /// </param>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="name"/> is empty/whitespace or <paramref name="phone"/> does not
    /// contain 9 to 15 digits.
    /// </exception>
    public void Update(string name, string phone)
    {
        EnsureNotDeleted();
        var oldName = Name;
        var oldPhone = Phone;
        SetDetails(name, phone);
        if (oldName == Name && oldPhone == Phone) return;
        Touch();
        Raise(new CustomerUpdatedDomainEvent(Id, oldName, oldPhone, Name, Phone));
    }

    /// <summary>
    /// Soft-deletes the customer and records the actor responsible for the deletion.
    /// </summary>
    /// <param name="deleteByUser">
    /// The user identifier responsible for the deletion.
    /// </param>
    public void Delete(string deleteByUser)
    {
        if (IsDelete) return;
        IsDelete = true;
        DeleteByUser = string.IsNullOrWhiteSpace(deleteByUser) ? "system" : deleteByUser.Trim();
        DeletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Strips non-digit characters from a phone number and validates the resulting length.
    /// </summary>
    /// <param name="phone">
    /// The raw phone number, in any format.
    /// </param>
    /// <returns>
    /// The digits-only phone number.
    /// </returns>
    /// <exception cref="DomainException">
    /// Thrown when the resulting digit string is shorter than 9 or longer than 15 characters.
    /// </exception>
    public static string NormalizePhone(string phone)
    {
        var normalized = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (normalized.Length is < 9 or > 15) throw new DomainException("Phone must contain 9 to 15 digits.");
        return normalized;
    }

    private void SetDetails(string name, string phone)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new DomainException("Customer name is required.") : name.Trim();
        Phone = NormalizePhone(phone);
        ReversedPhone = new string(Phone.Reverse().ToArray());
    }

    private void EnsureNotDeleted()
    {
        if (IsDelete) throw new DomainException("Deleted customers cannot be changed.");
    }
}
