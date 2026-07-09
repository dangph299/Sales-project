namespace Sales.Domain;

/// <summary>
/// Aggregate root for a sales order. Owns the order's lines, status lifecycle
/// (<see cref="OrderStatus"/>), and totals, and raises the domain events consumed to talk to
/// Inventory and for auditing.
/// </summary>
public sealed class Order : AggregateRoot
{
    private readonly List<OrderLine> _lines = [];
    private Order() { }
    private Order(Guid id, CustomerSnapshot customer)
    {
        Id = id;
        CustomerId = customer.Id;
        CustomerName = customer.Name;
        CustomerPhone = customer.Phone;
        CreatedAt = DateTimeOffset.UtcNow;
        Status = OrderStatus.Draft;
    }

    /// <summary>
    /// Gets the unique identifier of the customer this order was placed for.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Gets the customer's name as it was when the order was created.
    /// </summary>
    public string CustomerName { get; private set; } = null!;

    /// <summary>
    /// Gets the customer's phone number as it was when the order was created.
    /// </summary>
    public string CustomerPhone { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC instant the order was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the order's current lifecycle status.
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// Gets the reason Inventory rejected the reservation, or <see langword="null"/> if the order
    /// was never rejected.
    /// </summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Gets the order's lines.
    /// </summary>
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Gets the sum of all lines' quantities.
    /// </summary>
    public int TotalQuantity => _lines.Sum(x => x.Quantity);

    /// <summary>
    /// Gets the sum of all lines' totals.
    /// </summary>
    public Money Total => _lines.Aggregate(Money.Vnd(0), (sum, line) => sum + line.LineTotal);

    /// <summary>
    /// Creates a new <see cref="Order"/> aggregate in the <see cref="OrderStatus.Draft"/> status and
    /// raises <see cref="OrderCreatedDomainEvent"/>.
    /// </summary>
    /// <param name="customer">
    /// A snapshot of the customer the order is placed for.
    /// </param>
    /// <param name="lines">
    /// The initial lines to add. Must contain at least one line, with no product repeated.
    /// </param>
    /// <returns>
    /// The newly created draft order.
    /// </returns>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="lines"/> is empty or contains the same product more than once.
    /// </exception>
    public static Order Create(CustomerSnapshot customer, IEnumerable<OrderLineItem> lines)
    {
        var order = new Order(Guid.NewGuid(), customer);
        order.SetLines(lines, touch: false);
        order.Raise(new OrderCreatedDomainEvent(order.Id, order.CustomerId));
        return order;
    }

    /// <summary>
    /// Replaces this order's lines with a new set. Only allowed while the order is
    /// <see cref="OrderStatus.Draft"/>. Raises <see cref="OrderLinesReplacedDomainEvent"/>.
    /// </summary>
    /// <param name="lines">
    /// The new lines to set. Must contain at least one line, with no product repeated.
    /// </param>
    /// <exception cref="DomainException">
    /// Thrown when the order is not in the <see cref="OrderStatus.Draft"/> status, or
    /// <paramref name="lines"/> is empty or contains the same product more than once.
    /// </exception>
    public void ReplaceLines(IEnumerable<OrderLineItem> lines)
    {
        EnsureDraft();
        SetLines(lines, touch: true);
        Raise(new OrderLinesReplacedDomainEvent(Id, TotalQuantity, Total.Amount));
    }

    private void SetLines(IEnumerable<OrderLineItem> lines, bool touch)
    {
        var requested = lines.ToList();
        if (requested.Count == 0) throw new DomainException("Order needs at least one line.");
        if (requested.Select(x => x.Product.Id).Distinct().Count() != requested.Count)
            throw new DomainException("A product can occur only once in an order.");

        foreach (var line in _lines.Where(existing => requested.All(x => x.Product.Id != existing.ProductId)).ToArray())
            _lines.Remove(line);

        foreach (var (product, quantity, discountPercent) in requested)
        {
            var existing = _lines.SingleOrDefault(x => x.ProductId == product.Id);
            if (existing is null) _lines.Add(OrderLine.Create(Id, product, quantity, discountPercent));
            else existing.ReplaceWith(product, quantity, discountPercent);
        }

        if (touch) Touch();
    }

    /// <summary>
    /// Moves the order from <see cref="OrderStatus.Draft"/> to <see cref="OrderStatus.PendingInventory"/>
    /// and raises <see cref="OrderConfirmationRequestedDomainEvent"/> so Inventory can reserve stock.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown when the order is not in the <see cref="OrderStatus.Draft"/> status.
    /// </exception>
    public void RequestConfirmation()
    {
        EnsureDraft();
        Status = OrderStatus.PendingInventory;
        Touch();
        Raise(new OrderConfirmationRequestedDomainEvent(Id,
            _lines.Select(x => new OrderLineReservation(x.ProductId, x.Sku, x.Quantity)).ToArray()));
    }

    /// <summary>
    /// Moves the order from <see cref="OrderStatus.PendingInventory"/> to <see cref="OrderStatus.Confirmed"/>
    /// after Inventory reserves stock, and raises <see cref="OrderConfirmedDomainEvent"/>.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown when the order is not in the <see cref="OrderStatus.PendingInventory"/> status.
    /// </exception>
    public void MarkReserved()
    {
        if (Status != OrderStatus.PendingInventory) throw new DomainException("Order is not awaiting inventory.");
        Status = OrderStatus.Confirmed;
        Touch();
        Raise(new OrderConfirmedDomainEvent(Id));
    }

    /// <summary>
    /// Moves the order from <see cref="OrderStatus.PendingInventory"/> to <see cref="OrderStatus.InventoryRejected"/>
    /// after Inventory rejects the reservation, and raises <see cref="OrderInventoryRejectedDomainEvent"/>.
    /// </summary>
    /// <param name="reason">
    /// The reason Inventory rejected the reservation.
    /// </param>
    /// <exception cref="DomainException">
    /// Thrown when the order is not in the <see cref="OrderStatus.PendingInventory"/> status.
    /// </exception>
    public void RejectInventory(string reason)
    {
        if (Status != OrderStatus.PendingInventory) throw new DomainException("Order is not awaiting inventory.");
        Status = OrderStatus.InventoryRejected;
        RejectionReason = reason;
        Touch();
        Raise(new OrderInventoryRejectedDomainEvent(Id, reason));
    }

    /// <summary>
    /// Moves the order to <see cref="OrderStatus.Cancelled"/> and raises <see cref="OrderUndoComfirmedDomainEvent"/>
    /// so Inventory can release any reserved stock.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown when the order is <see cref="OrderStatus.Confirmed"/> or <see cref="OrderStatus.PendingInventory"/>.
    /// </exception>
    public void Cancel()
    {
        if (Status == OrderStatus.Confirmed || Status == OrderStatus.PendingInventory) throw new DomainException("Confirmed or pending inventory orders cannot be cancelled.");
        Status = OrderStatus.Cancelled;
        Touch();
    }

    public void UndoConfirmed()
    {
        if (Status != OrderStatus.Confirmed) throw new DomainException("Only confirmed orders can be undone.");
        Status = OrderStatus.Draft;
        Touch();
        Raise(new OrderUndoComfirmedDomainEvent(Id));
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft) throw new DomainException("Only a draft order can be edited.");
    }
}
