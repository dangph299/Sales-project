# Repository Rules

## Contracts

- Repository interfaces live in `<Service>.Domain/Repositories/`.
- They return aggregates or ids only. Never DTOs, projections, `IQueryable`, `DbSet`, or `Expression`.
- Generic base: `IRepository<T> where T : AggregateRoot<Guid>`.
- Aggregate-specific lookups extend it: `IOrderRepository.GetWithLinesAsync`, `IProductRepository.GetBySkuAsync`.
- `Sales.Architecture.Tests.Domain_repositories_do_not_expose_queryables` enforces this.

## Implementations

- Live in `<Service>.Infrastructure/Repositories/`, `sealed`, primary constructor taking the DbContext.
- `Repository<T>` is the shared Sales base; concrete repositories derive from it and use the protected `Db` field.
- Always `SingleOrDefaultAsync` for by-id lookups — never `FirstOrDefault` on a unique key.
- Eager-load children explicitly with `.Include(...)` when the caller mutates them (`GetWithLinesAsync`, `GetWithVariantsAsync`). A handler that calls aggregate behavior on children **must** use the include-aware method.
- Bulk loads deduplicate ids (`ids.Distinct().ToList()`) and use `Contains` for a single round trip. Never loop `GetByIdAsync`.
- `IgnoreQueryFilters()` only where a lookup must see soft-deleted rows (`GetBySkuAsync`, `GetByProductCodeAsync`) — never as a general escape hatch.
- Repositories do **not** call `SaveChangesAsync`. That is the unit of work's job.

## Default interface implementations

`IProductRepository` declares several members with default implementations so the interface can grow without breaking test doubles. When adding a member:

- give it a safe default in the interface,
- override it in `ProductRepository` with the real query.

## Reads vs. writes

| Need | Use |
|---|---|
| Load an aggregate to change it | repository |
| Return data to a client | read service (`I*ReadService`) |
| Aggregate/paginate/join for display | read service with `AsNoTracking()` and explicit projection |

Never load an aggregate through a repository just to map it for a query response.

## Unit of work

- `IUnitOfWork.SaveChangesAsync` is the only commit point in a Sales command handler.
- `UnitOfWork` (Sales) and `InventoryUnitOfWork` delegate to their `DbContext`.
- Inventory command handlers do **not** call `SaveChangesAsync`; `InventoryTransactionBehavior` does it inside the serializable transaction.
- Never inject a `DbContext` into an Application-layer type.

## Related

- [database-rule.md](database-rule.md)
- [cqrs-rule.md](cqrs-rule.md)
