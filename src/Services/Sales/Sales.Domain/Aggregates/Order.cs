namespace Sales.Domain;

/// <summary>
/// Aggregate root for a sales order. Owns the order's lines, status lifecycle
/// (<see cref="OrderStatus"/>), and totals, and raises the domain events consumed to talk to
/// Inventory and for auditing.
/// </summary>
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderLine> _lines = [];
    private Order() { }
    private Order(Guid id, string orderCode, OrderCustomerSnapshot orderCustomerSnapshot)
    {
        Id = id;
        OrderCode = ProductCodeRules.Normalize(orderCode, "Order code");
        ApplyInitialCustomerSnapshot(orderCustomerSnapshot);
        CreatedAt = DateTimeOffset.UtcNow;
        Status = OrderStatus.Draft;
    }

    /// <summary>
    /// Gets the backend-assigned business code identifying this order to users.
    /// </summary>
    public string OrderCode { get; private set; } = null!;

    /// <summary>
    /// Gets the customer this order was originally placed for.
    /// </summary>
    /// <remarks>
    /// Traceability only. It is assigned once, when the order is created, and
    /// <see cref="UpdateCustomerSnapshot"/> cannot change it — editing the customer details on an
    /// order does not re-point the order at a different customer.
    /// </remarks>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Gets the customer's name as recorded on this order.
    /// </summary>
    public string CustomerName { get; private set; } = null!;

    /// <summary>
    /// Gets the customer's phone number as recorded on this order, in the format it was entered in.
    /// Display only; searches read <see cref="NormalizedCustomerPhone"/> and
    /// <see cref="ReversedCustomerPhone"/>.
    /// </summary>
    public string CustomerPhone { get; private set; } = null!;

    /// <summary>
    /// Gets the digits-only form of <see cref="CustomerPhone"/>, which exact and prefix phone
    /// searches match against.
    /// </summary>
    public string NormalizedCustomerPhone { get; private set; } = null!;

    /// <summary>
    /// Gets <see cref="NormalizedCustomerPhone"/> reversed, which is what a search matches against
    /// when it matches the end of a phone number.
    /// </summary>
    public string ReversedCustomerPhone { get; private set; } = null!;

    /// <summary>
    /// Gets the customer's email as recorded on this order, or <see langword="null"/> when none was given.
    /// </summary>
    public string? CustomerEmail { get; private set; }

    /// <summary>
    /// Gets the customer's address as recorded on this order, or <see langword="null"/> when none was given.
    /// </summary>
    public string? CustomerAddress { get; private set; }

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
    /// <param name="orderCode">Backend-assigned business code for the new order.</param>
    /// <param name="orderCustomerSnapshot">Customer details to record on the order, taken from the create request rather than from the customer row.</param>
    /// <param name="orderLineItems">Initial lines to add. Must contain at least one line, with no product repeated.</param>
    /// <returns>Newly created draft order.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="orderCode"/> is empty or malformed, or <paramref name="orderLineItems"/> is empty or contains the same product variant more than once.</exception>
    public static Order Create(
        string orderCode,
        OrderCustomerSnapshot orderCustomerSnapshot,
        IEnumerable<OrderLineItem> orderLineItems)
    {
        var order = new Order(Guid.NewGuid(), orderCode, orderCustomerSnapshot);
        order.SetLines(orderLineItems, touch: false);
        order.Raise(new OrderCreatedDomainEvent(order.Id, order.CustomerId));
        return order;
    }

    /// <summary>
    /// Replaces the customer details recorded on this order.
    /// </summary>
    /// <remarks>
    /// Changes only this order. The customer row is never read or written here, no customer is
    /// created, and <see cref="CustomerId"/> keeps pointing at whoever the order was first placed
    /// for — the private mutator this calls has no access to it. After this runs the order may
    /// legitimately disagree with the current customer record.
    /// </remarks>
    /// <param name="orderCustomerSnapshot">New customer details to record.</param>
    /// <exception cref="DomainException">Thrown when the order is not in the <see cref="OrderStatus.Draft"/> status.</exception>
    public void UpdateCustomerSnapshot(OrderCustomerSnapshot orderCustomerSnapshot)
    {
        EnsureDraft();
        ApplyEditableCustomerSnapshot(orderCustomerSnapshot);
        Touch();
    }

    private void ApplyInitialCustomerSnapshot(OrderCustomerSnapshot orderCustomerSnapshot)
    {
        CustomerId = orderCustomerSnapshot.CustomerId;
        ApplyEditableCustomerSnapshot(orderCustomerSnapshot);
    }

    /// <summary>
    /// Writes every customer field an edit is allowed to touch. Deliberately excludes
    /// <see cref="CustomerId"/>, which only <see cref="ApplyInitialCustomerSnapshot"/> sets.
    /// </summary>
    /// <remarks>
    /// This is the only writer of the three phone columns, so they cannot drift apart: they always
    /// come from one <see cref="OrderCustomerPhone"/> built from a single input.
    /// </remarks>
    private void ApplyEditableCustomerSnapshot(OrderCustomerSnapshot orderCustomerSnapshot)
    {
        CustomerName = orderCustomerSnapshot.Name;
        CustomerPhone = orderCustomerSnapshot.Phone.DisplayValue;
        NormalizedCustomerPhone = orderCustomerSnapshot.Phone.NormalizedValue;
        ReversedCustomerPhone = orderCustomerSnapshot.Phone.ReversedValue;
        CustomerEmail = orderCustomerSnapshot.Email;
        CustomerAddress = orderCustomerSnapshot.Address;
    }

    /// <summary>
    /// Replaces this order's lines with a new set. Only allowed while the order is
    /// <see cref="OrderStatus.Draft"/>. Raises <see cref="OrderLinesReplacedDomainEvent"/>.
    /// </summary>
    /// <param name="lines">New lines to set. Must contain at least one line, with no product variant repeated.</param>
    /// <exception cref="DomainException">Thrown when the order is not in the <see cref="OrderStatus.Draft"/> status, or <paramref name="lines"/> is empty or contains the same product variant more than once.</exception>
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
        if (requested.Select(x => x.Product.ProductVariantId).Distinct().Count() != requested.Count)
            throw new DomainException("A product variant can occur only once in an order.");

        foreach (var line in _lines.Where(existing => requested.All(x => x.Product.ProductVariantId != existing.ProductVariantId)).ToArray())
            _lines.Remove(line);

        foreach (var (product, quantity, discountPercent) in requested)
        {
            var existing = _lines.SingleOrDefault(x => x.ProductVariantId == product.ProductVariantId);
            if (existing is null) _lines.Add(OrderLine.Create(Id, product, quantity, discountPercent));
            else existing.ReplaceWith(product, quantity, discountPercent);
        }

        if (touch) Touch();
    }

    /// <summary>
    /// Moves the order from <see cref="OrderStatus.Draft"/> to <see cref="OrderStatus.PendingInventory"/>
    /// and raises <see cref="OrderConfirmationRequestedDomainEvent"/> so Inventory can reserve stock.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the order is not in the <see cref="OrderStatus.Draft"/> status.</exception>
    public void RequestConfirmation()
    {
        EnsureDraft();
        Status = OrderStatus.PendingInventory;
        Touch();
        Raise(new OrderConfirmationRequestedDomainEvent(Id,
            _lines.Select(x => new OrderLineReservation(x.ProductVariantId, x.Sku, x.Quantity)).ToArray()));
    }

    /// <summary>
    /// Moves the order from <see cref="OrderStatus.PendingInventory"/> to <see cref="OrderStatus.Confirmed"/>
    /// after Inventory reserves stock, and raises <see cref="OrderConfirmedDomainEvent"/>.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the order is not in the <see cref="OrderStatus.PendingInventory"/> status.</exception>
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
    /// <param name="reason">Reason Inventory rejected the reservation.</param>
    /// <exception cref="DomainException">Thrown when the order is not in the <see cref="OrderStatus.PendingInventory"/> status.</exception>
    public void RejectInventory(string reason)
    {
        if (Status != OrderStatus.PendingInventory) throw new DomainException("Order is not awaiting inventory.");
        Status = OrderStatus.InventoryRejected;
        RejectionReason = reason;
        Touch();
        Raise(new OrderInventoryRejectedDomainEvent(Id, reason));
    }

    /// <summary>
    /// Cancels an open order that has not changed before the expiration cutoff.
    /// </summary>
    /// <param name="orderUpdatedBefore">Latest allowed order update time.</param>
    /// <returns><see langword="true"/> when the order was cancelled; otherwise <see langword="false"/>.</returns>
    public bool CancelDueToExpiration(DateTimeOffset orderUpdatedBefore)
    {
        if (!CanBeCancelledDueToExpiration())
        {
            return false;
        }

        if (UpdatedAt > orderUpdatedBefore)
        {
            return false;
        }

        var shouldReleaseInventoryReservation = Status == OrderStatus.PendingInventory;

        Status = OrderStatus.Cancelled;
        Touch();
        if (shouldReleaseInventoryReservation)
        {
            Raise(new OrderUndoComfirmedDomainEvent(Id));
        }

        return true;
    }

    /// <summary>
    /// Moves a confirmed order back to <see cref="OrderStatus.Draft"/> and raises <see cref="OrderUndoComfirmedDomainEvent"/>
    /// so Inventory can release any reserved stock.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the order is <see cref="OrderStatus.Confirmed"/> or <see cref="OrderStatus.PendingInventory"/>.</exception>
    public void Cancel()
    {
        if (Status == OrderStatus.Confirmed || Status == OrderStatus.PendingInventory) throw new DomainException("Confirmed or pending inventory orders cannot be cancelled.");
        Status = OrderStatus.Cancelled;
        Touch();
    }

    public void UndoConfirmed()
    {
        if (Status != OrderStatus.Confirmed) throw new DomainException("Only confirmed orders can be undone.");
        if (_lines.Any(x => x.IsSellThroughDiscontinued))
        {
            throw new DomainException("Orders confirmed with discontinued product variants cannot be undone.");
        }

        Status = OrderStatus.Draft;
        Touch();
        Raise(new OrderUndoComfirmedDomainEvent(Id));
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft) throw new DomainException("Only a draft order can be edited.");
    }

    private bool CanBeCancelledDueToExpiration()
    {
        return Status is OrderStatus.Draft or OrderStatus.PendingInventory or OrderStatus.InventoryRejected;
    }
}
