# Backend Coding Rules

Applies to every C# file under `src/`.

## Language and style

- Target `net10.0`. Nullable reference types and implicit usings are on via `Directory.Build.props`. Do not disable them per project.
- 4-space indent, file-scoped namespaces, one public type per file, file named after the type.
- `sealed` on every concrete class unless it is designed for inheritance.
- Prefer primary constructors for services, handlers, repositories, and background services.
- **Do not** use primary constructors in `Sales.Api`/`Inventory.Api` controllers that take more than one dependency — use explicit `private readonly` fields (see `CustomersController`, `OrdersController`, `ProductsController`).
- Use collection expressions (`[]`, `[a, b]`) instead of `new List<T> { ... }` / `Array.Empty<T>()`.
- Use `var` when the type is obvious from the right-hand side.
- No regions. No commented-out code.

## Naming

Follow [naming.md](naming.md).

## Comments and XML docs

- Every public type and member in `src/Shared/**` and every public port/contract gets an XML `<summary>`.
- Write comments that explain **why**, never what. If a comment restates the code, delete it.
- Document non-obvious trade-offs inline (see `LoggingBehavior`, `IntegrationEventHandler`, `InventoryTransactionBehavior` for the expected tone).

## Async

Follow [async-rule.md](async-rule.md).

## Immutability

- Commands, queries, DTOs, domain events, and integration events are `sealed record`.
- Aggregate/entity state uses `{ get; private set; }`. Never a public setter on a domain type.
- Collections on aggregates are private `List<T>` exposed as `IReadOnlyCollection<T>` via `.AsReadOnly()`.

## Guard clauses

- Domain invariants throw `DomainException`.
- Argument contract violations in shared code throw via `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`.
- Never return `null` to mean "invalid"; return `null` only for "not found" from a read/lookup.

## Forbidden

- `DateTime.Now` / `DateTimeOffset.Now`. Use `IClock.UtcNow` in Application and Infrastructure services; `DateTimeOffset.UtcNow` is only allowed inside Domain aggregates that stamp their own timestamps.
- `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` — except in the one documented shutdown hook in `StartupTaskExtensions`.
- `IQueryable` or `DbSet` crossing out of Infrastructure.
- EF Core, Kafka, Redis, Hangfire, ASP.NET Core types in Domain or Application.
- Magic strings for topics, consumer groups, headers, error codes, queue names, job ids — use the shared constant classes.
- `catch (Exception) { }` without either logging or rethrowing.
- New Minimal API endpoints for business APIs. Controllers only.

## Related

- [checklist.md](checklist.md)
- [architecture.md](architecture.md)
