# Aggregate Rules

## Shape

```csharp
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }                    // EF only
    private Order(Guid id, CustomerSnapshot customer) { ... }

    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Order Create(CustomerSnapshot customer, IEnumerable<OrderLineItem> lines) { ... }

    public void RequestConfirmation() { ... }
}
```

Rules:

1. `sealed`, derives from `AggregateRoot<Guid>`.
2. Two private constructors: parameterless for EF, parameterized for the factory.
3. Public `static Create(...)` factory that raises the creation domain event.
4. All properties `{ get; private set; }`.
5. Child collections are private `List<T>` exposed as `IReadOnlyCollection<T>`.
6. Behavior methods only — no anemic setters.

## Transitions

- Guard the current status first and throw `DomainException` with a business message.
- Make idempotent transitions return early rather than throw (`if (Status == Published) return;`).
- Call `Touch()` after mutating; raise the domain event last.
- A transition that must report success/failure without throwing returns `bool` (`Reservation.Release`, `Reservation.Reactivate`, `Order.CancelDueToExpiration`).

## Child entities

- `internal static Create(...)` and `internal void ReplaceWith(...)` so only the owning root can call them.
- Replace-set semantics: remove children absent from the new set, update matching ones, add new ones (`Order.SetLines`, `Reservation.SetLines`).
- Enforce uniqueness inside the set (`A product variant can occur only once in an order.`).

## Cross-aggregate work

- Never load or mutate another aggregate from inside an aggregate.
- Orchestration across aggregates belongs to the command handler.
- Data from another aggregate enters as a snapshot value object built by the handler.

## Concurrency

- `Version` is mapped as the EF concurrency token (`entity.Property(x => x.Version).IsConcurrencyToken()`).
- Command handlers that mutate an existing aggregate compare the caller's expected version before invoking behavior (`OrderCommandSupport.LoadAndCheck`) and throw `ConflictException(currentVersion)` on mismatch.

## Current aggregates

| Aggregate | Root | Children | Status enum | Soft delete |
|---|---|---|---|---|
| `Order` | yes | `OrderLine` | `OrderStatus` | no |
| `Product` | yes | `ProductVariant` | `EProductStatus` / `EProductVariantStatus` | yes |
| `Customer` | yes | — | `ECustomerStatus` | yes |
| `Category` | yes | — | `ECategoryStatus` | yes |
| `Reservation` | yes | `ReservationLine` | `ReservationStatus` | no |
| `InventoryItem` | plain `IEntity<Guid>` keyed by `ProductId` | — | — | no |

`Color` and `Size` are seeded reference entities, not aggregates. They are never mutated at runtime.

## Related

- [ddd-rule.md](ddd-rule.md)
- [entity-rule.md](entity-rule.md)
