namespace Sales.Domain;

/// <summary>
/// The customer details an order records for itself, independent of the customer the order was
/// first matched to.
/// </summary>
/// <remarks>
/// This is a snapshot, not a view: once an order is created, renaming, re-numbering or deleting the
/// customer leaves the order's copy alone, and editing the order's copy leaves the customer alone.
/// The type deliberately has no reference to <see cref="Customer"/> so neither can drift into the
/// other.
/// </remarks>
public sealed record OrderCustomerSnapshot
{
    private OrderCustomerSnapshot(
        Guid customerId,
        string name,
        OrderCustomerPhone phone,
        string? email,
        string? address)
    {
        CustomerId = customerId;
        Name = name;
        Phone = phone;
        Email = email;
        Address = address;
    }

    /// <summary>
    /// Gets the customer this order was originally placed for, kept for traceability only.
    /// </summary>
    public Guid CustomerId { get; }

    /// <summary>
    /// Gets the customer's name as recorded on the order.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the customer's phone number as recorded on the order, in all three of its forms.
    /// </summary>
    public OrderCustomerPhone Phone { get; }

    /// <summary>
    /// Gets the customer's email as recorded on the order, or <see langword="null"/> when none was given.
    /// </summary>
    public string? Email { get; }

    /// <summary>
    /// Gets the customer's address as recorded on the order, or <see langword="null"/> when none was given.
    /// </summary>
    public string? Address { get; }

    /// <summary>
    /// Creates a validated snapshot from the details supplied with an order request.
    /// </summary>
    /// <param name="customerId">Customer the order is traced back to.</param>
    /// <param name="customerName">Customer's name as it should appear on this order.</param>
    /// <param name="customerPhone">Customer's phone number, in any format containing 9 to 15 digits.</param>
    /// <param name="customerEmail">Customer's email, or <see langword="null"/>. Blank is stored as <see langword="null"/>.</param>
    /// <param name="customerAddress">Customer's address, or <see langword="null"/>. Blank is stored as <see langword="null"/>.</param>
    /// <returns>Validated snapshot.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="customerId"/> is empty, <paramref name="customerName"/> is empty/whitespace, or <paramref name="customerPhone"/> does not contain 9 to 15 digits.</exception>
    public static OrderCustomerSnapshot Create(
        Guid customerId,
        string customerName,
        string customerPhone,
        string? customerEmail,
        string? customerAddress)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Customer id is required.");
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new DomainException("Customer name is required.");
        }

        return new OrderCustomerSnapshot(
            customerId,
            customerName.Trim(),
            OrderCustomerPhone.Create(customerPhone),
            string.IsNullOrWhiteSpace(customerEmail) ? null : customerEmail.Trim(),
            string.IsNullOrWhiteSpace(customerAddress) ? null : customerAddress.Trim());
    }
}
