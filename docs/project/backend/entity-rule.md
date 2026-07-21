# Entity Rules

## Domain entities

- Derive from `Entity<Guid>` (child) or `AggregateRoot<Guid>` (root).
- Private parameterless constructor for EF; private parameterized constructor for factories.
- All properties `{ get; private set; }`. Initialize non-nullable reference properties with `= null!;`.
- Factory and mutation methods on child entities are `internal` so only the owning aggregate can call them.
- Never annotate domain types with EF attributes. All mapping lives in `IEntityTypeConfiguration<T>`.

## EF configuration rules

Every entity gets a `sealed class <Entity>Configuration : IEntityTypeConfiguration<T>` in `<Service>.Infrastructure/Persistence/Configurations/`, applied by `ApplyConfigurationsFromAssembly`.

Required in each configuration:

- `entity.ToTable("snake_case_plural")`.
- `entity.HasKey(...)`.
- `HasMaxLength` on every string column.
- `HasConversion<string>().HasMaxLength(32)` on every persisted enum.
- `entity.Property(x => x.Version).IsConcurrencyToken()` on every versioned aggregate.
- `entity.HasQueryFilter(x => !x.IsDelete)` on every soft-deletable entity.
- `entity.Ignore(...)` for computed domain properties (`Order.Total`, `Order.TotalQuantity`, `OrderLine.LineTotal`, `Product.Sku`, `Product.IsActive`).
- `entity.Navigation(x => x.Children).UsePropertyAccessMode(PropertyAccessMode.Field)` for private backing-list collections.
- `ValueConverter<Money, decimal>` with `HasColumnType("numeric(18,0)")` for money columns.

## Indexes

- Unique indexes on soft-deletable tables **must** carry `.HasFilter("NOT \"IsDelete\"")`, otherwise a deleted row keeps reserving its code and the conflict is invisible behind the query filter.
- Add an index for every column used in a `WHERE` or `ORDER BY` on a search path.
- Text search on `Name` uses `HasMethod("gin").HasOperators("gin_trgm_ops")` (requires `HasPostgresExtension("pg_trgm")`).

## Infrastructure-owned entities

- `OutboxMessage` and `InboxMessage` live in `BuildingBlocks.Infrastructure`; each service owns its own configuration. Do not add service-specific fields to the shared class — widen it as a nullable superset and constrain it per service (`Consumer` is `IsRequired()` in Sales, nullable in Inventory).
- `ApplicationUser` and `RefreshToken` are Sales infrastructure identity types, not domain types.
- Audit-related types and Outbox/Inbox rows are excluded from audit generation via `options.IgnoreEntity<T>()`.

## Reference data

- Seeded entities (`Color`, `Size`, the `Uncategorized` category) use `entity.HasData(...)` with stable GUIDs declared in `Persistence/SeedData/ReferenceData/*ReferenceDataIds.cs`.
- Never hardcode a seeded GUID outside those files. Clients resolve ids through `GET /api/common/colors|sizes` and `GET /api/categories`.

## Related

- [database-rule.md](database-rule.md)
- [migration-rule.md](migration-rule.md)
- [aggregate-rule.md](aggregate-rule.md)
