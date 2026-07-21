# Database Conventions

## Stores

| Store | Owner | Contents |
|---|---|---|
| PostgreSQL `sales` | Sales.Api | `categories`, `products`, `product_variants`, `colors`, `sizes`, `customers`, `orders`, `order_lines`, `refresh_tokens`, ASP.NET Identity tables, `outbox_messages`, `inbox_messages`, 3 code sequences |
| PostgreSQL `inventory` | Inventory.Api | `inventory_items`, `reservations`, `reservation_lines`, `outbox_messages`, `inbox_messages` |
| PostgreSQL `hangfire` | Sales.Api | Hangfire job storage |
| MongoDB `audit` | AuditLog.Worker | `events` collection |
| Redis | Sales.Api | product read cache, cleanup lock |

Databases are created by `docker/seed/postgres-init.sql`. Provider is Npgsql 10.

## Naming

- Tables: `snake_case`, plural.
- Columns: PascalCase — EF's default. No snake-case naming convention is applied, so raw SQL must quote identifiers (`NOT "IsDelete"`).
- Sequences: `<entity>_code_seq`.

## Mapping

All mapping lives in `IEntityTypeConfiguration<T>` classes under `Persistence/Configurations/`, applied by `ApplyConfigurationsFromAssembly`. No data annotations on domain types.

Conventions applied consistently:

- `HasMaxLength` on every string column (`32` codes, `100` names of reference data, `200` entity names, `254` email, `500` address, `1000` description, `96` SKU, `128` actor columns).
- Enums: `HasConversion<string>().HasMaxLength(32)` (24 for `ReservationStatus`).
- Money: `ValueConverter<Money, decimal>` + `HasColumnType("numeric(18,0)")`.
- Concurrency: `Property(x => x.Version).IsConcurrencyToken()` on `Category`, `Product`, `ProductVariant`, `Customer`, `Order`, `InventoryItem`.
- Computed domain properties are `Ignore`d: `Order.Total`, `Order.TotalQuantity`, `OrderLine.LineTotal`, `Product.Sku`, `Product.IsActive`, and `Reservation.DomainEvents/UpdatedAt/Version`.
- Private backing collections: `Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field)`.
- Outbox payloads use `jsonb`.

## Soft delete

Applied to `Category`, `Product`, `ProductVariant`, `Customer`.

- `HasQueryFilter(x => !x.IsDelete)` hides deleted rows everywhere.
- Every unique index on these tables carries `.HasFilter("NOT \"IsDelete\"")`. Without it a deleted row keeps reserving its code forever while the query filter hides the row causing the conflict — a 409 against a record nothing can display. Migration `UniqueIndexesExcludeSoftDeleted` fixed this.
- `IgnoreQueryFilters()` is used only in `ProductRepository.GetBySkuAsync` / `GetByProductCodeAsync`.

## Indexes

| Table | Indexes |
|---|---|
| `categories` | `CategoryCode` u/f, `ParentCategoryId`, `Status`, `(Name, ParentCategoryId)` u/f, `Name` u where parent is null + not deleted |
| `products` | `ProductCode` u/f, `Name` GIN `gin_trgm_ops`, `CategoryId`, `Status` |
| `product_variants` | `Sku` u/f, `(ProductId, ColorId, SizeId)` u/f, `Status` |
| `colors` | `ColorCode` u, `Name` u |
| `sizes` | `Code` u, `SortOrder` u |
| `customers` | `CustomerCode` u/f, `NormalizedPhone` u/f, `Name` GIN `gin_trgm_ops`, `Phone`, `ReversedPhone`, `Status` |
| `orders` | `CreatedAt`, `(Status, UpdatedAt, Id)`, `CustomerName` GIN `gin_trgm_ops`, `CustomerPhone` |
| `order_lines` | `(OrderId, ProductVariantId)` u |
| `refresh_tokens` | `TokenHash` u |
| `inventory_items` | `Sku` u |
| `reservations` | `OrderId` u |
| `outbox_messages` | `(ProcessedAt, OccurredAt)`, `(DeadLetteredAt, NextAttemptAt, OccurredAt)`, `LockId` |
| `inbox_messages` | `(Status, DeadLetteredAt)` |

`u` = unique, `f` = filtered on `NOT "IsDelete"`. The `pg_trgm` extension is declared with `HasPostgresExtension("pg_trgm")`.

## Foreign keys

- `products.CategoryId` → `categories`, `Restrict`
- `categories.ParentCategoryId` → `categories`, `Restrict`
- `product_variants.ProductId` → `products`, `Cascade`
- `product_variants.ColorId/SizeId` → `colors`/`sizes`, `Restrict`
- `order_lines.OrderId` → `orders`, `Cascade`
- `reservation_lines.ReservationId` → `reservations`, default

Reference data and categories are never cascade-deleted; children of an aggregate are.

## Business code sequences

Declared on the model (Npgsql only, guarded by `Database.IsNpgsql()` so SQLite-backed tests can still build the schema) and allocated by `SequentialCodeGenerator`:

```sql
SELECT nextval('customer_code_seq'::regclass)
```

`nextval` is atomic and never returns the same value twice, so concurrent creates across any number of instances get distinct codes without locking. Codes are unique and monotonic, not gap-free.

## Transactions

- Sales: `SaveChangesAsync` is the transaction. The only explicit transaction is in `SalesInventoryEventProcessor`, to make the inbox insert and the order transition atomic.
- Inventory: every command runs in `IsolationLevel.Serializable` via `InventoryTransactionBehavior`.
- Maintenance cleanups run in their own transaction with an advisory lock (Inventory) or a Redis lock (Sales).

## MongoDB

Collection `audit.events`, upserted by `AuditId`. Indexes created idempotently at startup:

- `ux_events_audit_id` (unique)
- `ix_events_entity_time` — `EntityType`, `EntityId`, `OccurredAt desc`
- `ix_events_service_time` — `ServiceName`, `OccurredAt desc`
- `ix_events_correlation_id`

## Migrations

Sales has 12 migrations, Inventory 7, both under `Persistence/Migrations/`. Sales migrates at startup in `IdentitySeeder.SeedIdentityAsync`; Inventory in `RunStartupTasksAsync`. Mongo has no migrations.

## Related

- [database-schema.md](database-schema.md)
- Rules: [../project/backend/database-rule.md](../project/backend/database-rule.md), [../project/backend/migration-rule.md](../project/backend/migration-rule.md)
