# Migration Rules

## Creating a migration

```bash
dotnet ef migrations add <PascalCaseName> \
  --project src/Services/Sales/Sales.Infrastructure \
  --startup-project src/Services/Sales/Sales.Api \
  --output-dir Persistence/Migrations
```

Use the Inventory equivalent for `Inventory.Infrastructure` / `Inventory.Api`.

## Rules

- Name a migration after the change, in PascalCase, no date prefix (`UniqueIndexesExcludeSoftDeleted`, `SellThroughDiscontinuedOrderLine`).
- Always commit the migration, its `.Designer.cs`, and the updated `<Context>ModelSnapshot.cs` together.
- Never edit or delete an applied migration. Ship a new one.
- Never hand-write a migration that the model can generate. Declare it on the model (sequences, extensions, seed data) and scaffold.
- Review generated SQL before committing; scaffolding can drop and recreate an index unnecessarily.

## Data and seed changes

- Reference data changes go through `HasData` with the stable GUIDs in `Persistence/SeedData/ReferenceData/`.
- Custom data backfill or sequence seeding is added by hand inside the generated migration's `Up` (see `BackendAssignedEntityCodeSequences`).
- Every `Up` needs a working `Down` unless the change is genuinely irreversible; say so in a comment if it is.

## Applying migrations

- Sales applies migrations at startup inside `IdentitySeeder.SeedIdentityAsync` (`db.Database.MigrateAsync()`).
- Inventory applies them in `RunStartupTasksAsync`.
- AuditLog has no relational schema; MongoDB indexes are created idempotently by `MongoStartupService` → `IAuditWriter.EnsureIndexesAsync`.
- Never call `EnsureCreated()`.

## Breaking changes

- Additive first: add nullable column → backfill → make required in a later migration.
- A column rename is an add + copy + drop across releases, never an in-place rename, while old instances may still be running.
- A change to a persisted enum's **name** is a breaking data change (enums are stored as strings). Migrate the data explicitly.

## Compatibility with tests

Some infrastructure tests build the schema from the model against SQLite. Provider-specific model constructs must be guarded:

```csharp
if (Database.IsNpgsql()) { /* sequences, extensions */ }
```

## Related

- [database-rule.md](database-rule.md)
- [testing-rule.md](testing-rule.md)
