# Domain Layer Rules

## What belongs here

- Aggregates, entities, value objects, enums, domain events, domain exceptions, repository contracts, specifications, pure domain services.

## What never belongs here

- Persistence, messaging, caching, HTTP, logging, DI, configuration, mapping, validation frameworks.
- DTOs, request/response models, read models.
- Any type from another bounded context or from `BuildingBlocks.Application`/`Infrastructure`/`Web`.

## Repository contracts

```csharp
public interface IRepository<T> where T : AggregateRoot<Guid>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(T aggregate, CancellationToken cancellationToken = default);
    void Update(T aggregate);
    void Delete(T aggregate);
}
```

- Command-side only. Returns whole aggregates, never DTOs or projections.
- Never expose `IQueryable`, `DbSet`, or an `Expression` parameter.
- Aggregate-specific lookups extend `IRepository<T>` (`IOrderRepository`, `IProductRepository`).
- Inventory declares standalone repository interfaces (`IInventoryRepository`, `IReservationRepository`) because `InventoryItem` is not an `AggregateRoot`. Do not force it into `IRepository<T>`.

## Value objects

- `Money` is VND only, non-negative, rounded to 0 decimals with `MidpointRounding.AwayFromZero`. All monetary arithmetic goes through it.
- Snapshots (`ProductSnapshot`, `CustomerSnapshot`) validate in `Create` and reject inactive/incomplete source data.

## Enums

- Persisted as strings via `HasConversion<string>().HasMaxLength(32)`.
- Explicit numeric values on catalog/status enums (`Draft = 1`).
- `OrderStatus` and `ReservationStatus` intentionally use implicit values — do not renumber them; existing rows are stored by name.

## Normalization rules

- Business codes go through `ProductCodeRules.Normalize` — trimmed, upper-cased, must match `^[A-Z0-9][A-Z0-9_-]*$`.
- SKU = `ProductCode-ColorCode-SizeCode` via `ProductCodeRules.BuildSku`.
- Phone numbers: `Customer.NormalizePhone` strips non-digits and requires 9–15 digits. `ReversedPhone` is maintained for suffix search.

## Related

- [ddd-rule.md](ddd-rule.md)
- [repository-rule.md](repository-rule.md)
- Business rules: [../../tech/business/](../../tech/business/)
