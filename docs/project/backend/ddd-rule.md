# DDD Rules

## Aggregates

- One aggregate root per consistency boundary. Roots: `Order`, `Product`, `Customer`, `Category` (Sales); `Reservation` (Inventory).
- Aggregate roots derive from `AggregateRoot<Guid>`.
- Child entities derive from `Entity<Guid>` and are created/mutated **only** through their root: `OrderLine` via `Order`, `ProductVariant` via `Product`, `ReservationLine` via `Reservation`.
- Reference other aggregates by id only. Never hold an object reference to another root.
- Private parameterless constructor for EF materialization; all real construction goes through a `static Create(...)` factory.

## Invariants

- Every rule that must always hold lives in the aggregate, not in a handler, validator, or controller.
- Violations throw `DomainException` with a human-readable message.
- Every state transition is a named method (`RequestConfirmation`, `MarkReserved`, `Discontinue`, `Reactivate`), never a public status setter.
- A transition method first checks the current status, then mutates, then calls `Touch()`, then raises its domain event.

## Value objects

- Immutable, compared by value, `readonly record struct` or `sealed record`.
- Validate in the factory (`Money.Vnd`, `ProductSnapshot.Create`, `CustomerSnapshot.Create`).
- Use a snapshot value object when an aggregate must record another aggregate's data at a point in time. Never a live reference.

## Domain services and specifications

- Add a domain service only when behavior belongs to no single aggregate (`ProductCodeRules`).
- Reusable query predicates are `Specification<T>` subclasses returning `Expression<Func<T,bool>>` so EF can translate them. Compose with `.And(...)`.
- Domain-wide specs live in `<Service>.Domain/Services/Specifications/`; query-shape specs live in `<Service>.Infrastructure/Persistence/Specifications/`.

## Soft delete

- Soft-deletable aggregates carry `IsDelete`, `DeleteByUser`, `DeletedBy`, `DeletedAt`.
- `Delete(string deleteByUser)` is idempotent: return early when already deleted.
- Every mutating method calls a private `EnsureNotDeleted()` first.
- Deleted rows are hidden by an EF global query filter and excluded from unique indexes.

## Versioning

- `AggregateRoot.Version` starts at 1 and is incremented by `Touch()`.
- `Touch()` also updates `UpdatedAt`.
- Call `Touch()` exactly once per public mutation, at the end.
- `ProductVariant` keeps its own `Version`/`UpdatedAt` and its own private `Touch()` because it is not a root — do not "fix" this by making it an `AggregateRoot`.

## Forbidden in Domain

- EF Core, MediatR, Kafka, Redis, ASP.NET Core, `ILogger`, DI attributes.
- `IQueryable`, `DbSet`, LINQ-to-Entities.
- Reading configuration or the clock through anything other than `DateTimeOffset.UtcNow`.
- Any type from another bounded context.

## Related

- [aggregate-rule.md](aggregate-rule.md)
- [domain-rule.md](domain-rule.md)
- [event-rule.md](event-rule.md)
- Learning: [../../guides/06-ddd-in-this-project.md](../../guides/06-ddd-in-this-project.md)
