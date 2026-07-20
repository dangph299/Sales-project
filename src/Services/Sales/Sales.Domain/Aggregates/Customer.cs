namespace Sales.Domain;

/// <summary>
/// Aggregate root for a customer. Owns invariants around name/phone validity and raises the
/// domain events consumed for auditing.
/// </summary>
public sealed class Customer : AggregateRoot<Guid>
{
    private static long customerCodeSequence;

    private Customer() { }
    private Customer(Guid id, string customerCode, string name, string phone, string? email, string? address)
    {
        Id = id;
        CustomerCode = ProductCodeRules.Normalize(customerCode, "Customer code");
        SetDetails(name, phone, email, address);
        Status = ECustomerStatus.Normal;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string CustomerCode { get; private set; } = null!;

    /// <summary>
    /// Gets the customer's name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the customer's normalized phone number (digits only).
    /// </summary>
    public string Phone { get; private set; } = null!;

    public string NormalizedPhone { get; private set; } = null!;

    public string? Email { get; private set; }

    public string? Address { get; private set; }

    public ECustomerStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public string? UpdatedBy { get; private set; }

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

    public string? DeletedBy { get; private set; }

    /// <summary>
    /// Gets the UTC instant this customer was soft-deleted, or <see langword="null"/> if it is active.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    /// Creates a new <see cref="Customer"/> aggregate and raises <see cref="CustomerCreatedDomainEvent"/>.
    /// </summary>
    /// <param name="name">Customer's name.</param>
    /// <param name="phone">Customer's phone number, in any format containing 9 to 15 digits.</param>
    /// <returns>Newly created customer.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="name"/> is empty/whitespace or <paramref name="phone"/> does not contain 9 to 15 digits.</exception>
    public static Customer Create(string customerCode, string name, string phone, string? email = null, string? address = null)
    {
        var customer = new Customer(Guid.NewGuid(), customerCode, name, phone, email, address);
        customer.Raise(new CustomerCreatedDomainEvent(customer.Id, customer.Name, customer.Phone));
        return customer;
    }

    public static Customer Create(string name, string phone)
    {
        var sequence = Interlocked.Increment(ref customerCodeSequence);
        var customerCode = $"CUS{sequence:D6}";
        return Create(customerCode, name, phone);
    }

    /// <summary>
    /// Updates the customer's name and phone number. Raises <see cref="CustomerUpdatedDomainEvent"/>
    /// and increments <see cref="AggregateRoot.Version"/> only if a value actually changed.
    /// </summary>
    /// <param name="name">Customer's new name.</param>
    /// <param name="phone">Customer's new phone number, in any format containing 9 to 15 digits.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="name"/> is empty/whitespace or <paramref name="phone"/> does not contain 9 to 15 digits.</exception>
    public void Update(string name, string phone, string? email = null, string? address = null)
    {
        EnsureNotDeleted();
        var oldName = Name;
        var oldPhone = Phone;
        SetDetails(name, phone, email, address);
        if (oldName == Name && oldPhone == Phone) return;
        Touch();
        Raise(new CustomerUpdatedDomainEvent(Id, oldName, oldPhone, Name, Phone));
    }

    public void Suspend()
    {
        EnsureNotDeleted();
        if (Status == ECustomerStatus.Suspended) return;
        if (Status != ECustomerStatus.Normal) throw new DomainException("Only normal customers can be suspended.");

        Status = ECustomerStatus.Suspended;
        Touch();
    }

    public void Block()
    {
        EnsureNotDeleted();
        if (Status == ECustomerStatus.Blocked) return;
        if (Status is not (ECustomerStatus.Normal or ECustomerStatus.Suspended))
            throw new DomainException("Customer status transition is invalid.");

        Status = ECustomerStatus.Blocked;
        Touch();
    }

    public void Reactivate()
    {
        EnsureNotDeleted();
        if (Status == ECustomerStatus.Normal) return;
        if (Status != ECustomerStatus.Suspended) throw new DomainException("Only suspended customers can be reactivated.");

        Status = ECustomerStatus.Normal;
        Touch();
    }

    /// <summary>
    /// Soft-deletes the customer and records the actor responsible for the deletion.
    /// </summary>
    /// <param name="deleteByUser">User identifier responsible for the deletion.</param>
    public void Delete(string deleteByUser)
    {
        if (IsDelete) return;
        IsDelete = true;
        DeleteByUser = string.IsNullOrWhiteSpace(deleteByUser) ? "system" : deleteByUser.Trim();
        DeletedBy = DeleteByUser;
        DeletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Strips non-digit characters from a phone number and validates the resulting length.
    /// </summary>
    /// <param name="phone">Raw phone number, in any format.</param>
    /// <returns>Digits-only phone number.</returns>
    /// <exception cref="DomainException">Thrown when the resulting digit string is shorter than 9 or longer than 15 characters.</exception>
    public static string NormalizePhone(string phone)
    {
        var normalized = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (normalized.Length is < 9 or > 15) throw new DomainException("Phone must contain 9 to 15 digits.");
        return normalized;
    }

    private void SetDetails(string name, string phone, string? email, string? address)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new DomainException("Customer name is required.") : name.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? throw new DomainException("Phone is required.") : phone.Trim();
        NormalizedPhone = NormalizePhone(phone);
        ReversedPhone = new string(NormalizedPhone.Reverse().ToArray());
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
    }

    private void EnsureNotDeleted()
    {
        if (IsDelete) throw new DomainException("Deleted customers cannot be changed.");
    }
}
