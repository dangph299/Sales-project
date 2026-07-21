# Database Rules

## Stores

| Store | Owner | Purpose |
|---|---|---|
| PostgreSQL `sales` | Sales | catalog, customers, orders, identity, outbox, inbox |
| PostgreSQL `inventory` | Inventory | inventory items, reservations, outbox, inbox |
| PostgreSQL `hangfire` | Sales | Hangfire job storage |
| MongoDB `audit` | AuditLog | `events` collection of audit documents |
| Redis | Sales | product read cache, cleanup distributed lock |

- One database per bounded context. Never query another context's database.
- Connection strings come from `ConnectionStrings:<Name>`. Never hardcode one.

## DbContext

- One `DbContext` per bounded context, `sealed`, in `Persistence/DbContexts/`.
- `OnModelCreating` calls `ApplyConfigurationsFromAssembly` only; no inline `modelBuilder.Entity<T>()` mapping.
- `SalesDbContext` implements `IUnitOfWork` and overrides `SaveChangesAsync` to map domain events into the outbox before committing, then clears them and signals the publisher.
- `InventoryDbContext` buffers integration events in `_pending` via `Enqueue(envelope, topic)` and flushes them into the outbox on save.
- Register with `AddDbContext<T>((sp, options) => options.UseNpgsql(...).AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()))`.

## Query rules

- Reads are `AsNoTracking()`.
- Never `ToList()` before filtering, paging, or counting.
- Paginate with `Paging.Normalize(page, pageSize)` then `Skip/Take`, and count with `LongCountAsync` on the same filtered query.
- Bulk maintenance deletes use `ExecuteDeleteAsync`; bulk claim/lease updates use `ExecuteUpdateAsync`.
- Case-insensitive text search uses `EF.Functions.ILike(x.Name, $"%{value}%")` against a `gin_trgm_ops` index.
- Phone suffix search queries `ReversedPhone.StartsWith(reversedValue)`; prefix search queries `NormalizedPhone.StartsWith(value)`.
- Raw SQL is allowed only for sequence allocation and advisory locks, and must be parameterized (`SELECT nextval({name}::regclass)`).

## Transactions

- Sales: one `SaveChangesAsync` per command is the transaction. Only `SalesInventoryEventProcessor` opens an explicit transaction, to make the inbox insert and the order transition atomic.
- Inventory: every command runs in a `Serializable` transaction opened by `InventoryTransactionBehavior`; the handler must not commit or roll back itself.
- Never span a transaction across an HTTP call, a Kafka publish, or a cache write.
- Never publish to Kafka inside a business transaction — write to the outbox instead.

## Concurrency

- `Version` is the EF concurrency token on versioned aggregates.
- `DbUpdateConcurrencyException`, Postgres unique violations, and serialization failures are classified by `PostgresPersistenceExceptionClassifier` into shared error codes and surfaced as `409`.
- Never swallow a concurrency exception. Either let it bubble to the API or record it (outbox/inbox failure path).

## Business codes

- `CUS`/`PRD`/`CAT` codes are allocated from Postgres sequences via `SequentialCodeGenerator`. Sequences are declared on the model (Npgsql only) and seeded by migration.
- Codes are unique and monotonic, not gap-free. Never scan existing rows to compute the next code.
- Allocate the code **after** all validation, so a rejected request does not consume a number.

## Related

- [migration-rule.md](migration-rule.md)
- [entity-rule.md](entity-rule.md)
- Reference: [../../tech/database-conventions.md](../../tech/database-conventions.md)
