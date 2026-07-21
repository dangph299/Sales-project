# 6. DDD in This Project

## Purpose

Show how the tactical DDD patterns are actually used here — including where the project deviates from the textbook, and why.

## Aggregates

An aggregate is a consistency boundary: a cluster of objects that must be saved together and whose rules are enforced together.

| Aggregate root | Children | Guards |
|---|---|---|
| `Order` | `OrderLine` | status machine, line uniqueness, totals |
| `Product` | `ProductVariant` | status machine, colour/size uniqueness, SKU generation |
| `Customer` | — | phone normalization, status machine |
| `Category` | — | status machine, self-parenting |
| `Reservation` | `ReservationLine` | status machine, staleness |

The shape is consistent:

```csharp
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }                                    // EF materialization
    private Order(Guid id, CustomerSnapshot customer) { … } // real construction

    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Order Create(CustomerSnapshot customer, IEnumerable<OrderLineItem> lines) { … }
}
```

Every property is `private set`. The collection is a private `List<T>` exposed read-only. There is no way to put an `Order` into an invalid state from outside.

### Behaviour, not setters

```csharp
public void RequestConfirmation()
{
    EnsureDraft();
    Status = OrderStatus.PendingInventory;
    Touch();
    Raise(new OrderConfirmationRequestedDomainEvent(Id,
        _lines.Select(x => new OrderLineReservation(x.ProductVariantId, x.Sku, x.Quantity)).ToArray()));
}
```

Guard → mutate → `Touch()` → raise. That order matters: the event carries the post-change state, and `Touch()` bumps the version the event reports.

### Children are sealed off

`OrderLine.Create` and `OrderLine.ReplaceWith` are `internal`, so only `Order` can call them. You cannot obtain an `OrderLine` and change its quantity.

`Order.SetLines` implements replace-set semantics: remove lines absent from the new set, update matching ones, add new ones — preserving the identity of lines that survive an edit.

### Aggregates reference each other by id

An `Order` holds `CustomerId`, not a `Customer`. When it needs the customer's data it takes a **snapshot**:

```csharp
var customerSnapshot = CustomerSnapshot.Create(customer.Id, customer.Name, customer.Phone);
var order = Order.Create(customerSnapshot, orderLineItems);
```

Renaming a customer never rewrites their existing orders. The same applies to `ProductSnapshot` on order lines — price, SKU, name, colour, and size are all frozen at line creation.

## Value objects

Immutable, compared by value, validated in a factory.

```csharp
public readonly record struct Money
{
    public decimal Amount { get; }
    private Money(decimal amount) => Amount = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);

    public static Money Vnd(decimal amount)
    {
        if (amount < 0) throw new DomainException("Money cannot be negative.");
        return new Money(amount);
    }

    public static Money operator +(Money left, Money right) => Vnd(left.Amount + right.Amount);
}
```

Rounding and non-negativity are impossible to forget because there is no other way to construct one. Note there is no `-` operator: nothing in the domain subtracts money, so it is not offered.

## Domain events

Facts, past tense, business data only:

```csharp
public sealed record OrderConfirmedDomainEvent(Guid OrderId) : DomainEvent;
```

They know nothing about Kafka. `AggregateRoot` buffers them; `SalesDbContext.SaveChangesAsync` drains the buffer, maps what should leave the process, and writes outbox rows. See [07-domain-events-and-outbox.md](07-domain-events-and-outbox.md).

## Specifications

Reusable query rules expressed as expressions, so EF can translate them:

```csharp
public sealed class ActiveCustomerSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression() =>
        x => x.Status == ECustomerStatus.Normal && !x.IsDelete;
}
```

`Specification<T>.And` composes two by rewriting their parameter, so `OrderReadService` can build a filter from whichever criteria the caller supplied.

## Repositories

Command-side only, aggregates only:

```csharp
public interface IRepository<T> where T : AggregateRoot<Guid>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task AddAsync(T aggregate, CancellationToken ct = default);
    void Update(T aggregate);
    void Delete(T aggregate);
}
```

No `IQueryable`, no `Expression` parameter, no DTOs. An architecture test asserts it. Reads go through `I*ReadService` instead.

## Where this project deviates

Worth knowing, because they look like mistakes until you know the reason.

**`InventoryItem` is not an aggregate root.** It is an `IEntity<Guid>` keyed by `ProductId` with its own `Version`. It has no domain events and no child entities, so `AggregateRoot` would add ceremony. Inventory therefore declares standalone repository interfaces instead of using `IRepository<T>`.

**`ProductVariant` keeps its own `Version` and `Touch()`.** It is a child entity, but it is mapped with its own concurrency token because variants are edited independently through the product. Do not "fix" this by promoting it to a root — the product is the consistency boundary.

**Domain events are not dispatched in-process.** There is no `INotification` handler for `OrderConfirmedDomainEvent`. The only consumer is `DomainEventMapper`, and only for the two events that must leave the process. A domain event with no mapping is simply not published — intentional, not an oversight.

**`Reservation` ignores its inherited `Version` and `UpdatedAt`.** `ReservationConfiguration` explicitly ignores them; the aggregate uses `LastOrderVersion` (the *Sales* order version) for staleness instead of its own.

**Aggregates stamp their own timestamps with `DateTimeOffset.UtcNow`.** Everywhere else `IClock` is required. The domain has no dependencies, so it cannot take a clock — the trade-off is accepted and the timestamps are not business-critical.

## Common mistakes

| Mistake | Consequence |
|---|---|
| A public setter on an aggregate | invariants become optional |
| Loading another aggregate inside an aggregate | two consistency boundaries in one transaction |
| Holding a navigation to another root | cascade loads and accidental writes |
| Rules in the handler instead of the aggregate | invisible to domain tests, duplicated next time |
| Forgetting `Touch()` | the ETag does not change, so a stale client write succeeds |
| Raising an event before mutating | the event describes state that was never saved |
| Exposing the backing `List<T>` | outside code mutates the aggregate silently |

## Related

- [../guides/DDD-structure-guide.md](DDD-structure-guide.md) — deep dive (Vietnamese)
- [../tech/business/](../tech/business/) — the actual rules per aggregate
- [../project/backend/ddd-rule.md](../project/backend/ddd-rule.md), [aggregate-rule.md](../project/backend/aggregate-rule.md)
