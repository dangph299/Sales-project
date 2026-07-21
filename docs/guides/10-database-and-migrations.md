# 10. Database Design & Migrations

## Purpose

Explain how the domain model reaches PostgreSQL, the conventions the mappings follow, and the two design decisions most likely to surprise you: filtered unique indexes and database-allocated business codes.

## One database per context

| Database | Owner |
|---|---|
| `sales` | Sales.Api |
| `inventory` | Inventory.Api |
| `hangfire` | Sales.Api (job storage) |
| `audit` (MongoDB) | AuditLog.Worker |

Created by `docker/seed/postgres-init.sql`. No cross-database query and no cross-database foreign key exists. Inventory stores Sales ids as opaque `Guid`s.

## Mapping lives in configurations

Domain types carry no EF attributes. Every entity has an `IEntityTypeConfiguration<T>` applied by `ApplyConfigurationsFromAssembly`:

```csharp
public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> entity)
    {
        var money = new ValueConverter<Money, decimal>(x => x.Amount, x => Money.Vnd(x));

        entity.ToTable("product_variants");
        entity.HasKey(x => x.Id);
        entity.HasQueryFilter(x => !x.IsDelete);
        entity.HasIndex(x => x.Sku).IsUnique().HasFilter("NOT \"IsDelete\"");
        entity.Property(x => x.Price).HasConversion(money).HasColumnType("numeric(18,0)");
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Version).IsConcurrencyToken();
    }
}
```

This is why the domain stays framework-free: `ProductVariant` does not know it is stored.

Conventions applied everywhere: `HasMaxLength` on every string, enums as strings, `Money` through a converter, `Version` as the concurrency token, computed properties `Ignore`d, private backing collections accessed by field.

## Enums as strings

```csharp
entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
```

The column reads `'PendingInventory'`, not `1`. Reordering enum members is then safe; **renaming one is a breaking data change**. That trade is chosen deliberately: renames are rarer than reorders, and a readable column is worth a lot when debugging in `psql`.

## Soft delete, and the filtered-index trap

Four entities are soft-deletable: `Category`, `Product`, `ProductVariant`, `Customer`.

```csharp
entity.HasQueryFilter(x => !x.IsDelete);
```

Now every query silently excludes deleted rows — which creates a subtle failure with unique indexes. Consider a plain unique index on `ProductCode`:

1. `PRD001` is created, then deleted.
2. The row still exists and still owns `PRD001`.
3. Creating `PRD001` again fails with a unique violation.
4. You look for the conflicting product — and the query filter hides it.

A 409 against a record nothing can display. The fix, migration `UniqueIndexesExcludeSoftDeleted`:

```csharp
entity.HasIndex(x => x.ProductCode).IsUnique().HasFilter("NOT \"IsDelete\"");
```

**Every unique index on a soft-deletable table must carry that filter.** Note the quoted identifier — EF keeps PascalCase column names, so raw SQL fragments must quote them.

The same reasoning applies to `(ProductId, ColorId, SizeId)` on variants: without the filter, deleting a variant would permanently block re-adding that colour/size pair.

## Business codes from sequences

`CUS001`, `PRD007`, `CAT003` are allocated by PostgreSQL, not by the application:

```csharp
var sequenceNumber = await db.Database
    .SqlQuery<long>($"SELECT nextval({codeSequence.SequenceName}::regclass) AS \"Value\"")
    .SingleAsync(cancellationToken);
return codeSequence.Prefix + sequenceNumber.ToString("D3", CultureInfo.InvariantCulture);
```

Why not `MAX(code) + 1`? Because two concurrent creates would read the same maximum. `nextval` is atomic and never returns a value twice, so any number of API instances get distinct codes without locking.

The consequences are worth stating plainly:

- codes are unique and monotonic, **not gap-free** — a sequence does not roll back, so a number consumed by a failed create is skipped;
- allocation happens *after* validation, so a rejected request does not burn a number;
- the prefix and sequence name are declared once in `EntityCodeSequence` and read by the model, the migration, and the generators.

The sequences are declared on the model but guarded:

```csharp
if (Database.IsNpgsql())
    foreach (var codeSequence in EntityCodeSequence.All)
        builder.HasSequence<long>(codeSequence.SequenceName).StartsAt(1).IncrementsBy(1);
```

SQLite-backed tests build the schema from this same model and would otherwise fail outright. Any provider-specific construct needs the same guard.

## Indexes

Every filter and sort column on a search path has an index. Two techniques are worth knowing:

**Trigram text search.** `EF.Functions.ILike(x.Name, $"%{value}%")` cannot use a B-tree, so `Name` columns get a GIN index with `gin_trgm_ops` (enabled by `HasPostgresExtension("pg_trgm")`).

**Reversed phone.** Suffix search (`LIKE '%456'`) cannot use an index either. So `Customer` persists `ReversedPhone`, and a suffix search becomes a prefix search on the reversed column — which *can* use one.

## Snapshots instead of foreign keys

`orders.CustomerId` has no foreign key. `order_lines` has none to `product_variants`. That is deliberate: the order stores a **snapshot** of the customer and the product at the time it was placed, and it must survive the catalog changing or a customer being deleted.

Where a real containment relationship exists, the foreign key is there with the right cascade: `order_lines` → `orders` cascade, `product_variants` → `products` cascade, but `products` → `categories` and variants → colours/sizes are `Restrict` so reference data cannot be deleted out from under live rows.

## Migrations

```bash
dotnet ef migrations add UniqueIndexesExcludeSoftDeleted \
  --project src/Services/Sales/Sales.Infrastructure \
  --startup-project src/Services/Sales/Sales.Api \
  --output-dir Persistence/Migrations
```

Rules:

- name after the change, PascalCase, no date prefix (EF adds the timestamp);
- commit the migration, its `.Designer.cs`, **and** the updated model snapshot together — a missing snapshot makes the next migration wrong;
- never edit or delete an applied migration; ship a new one;
- declare on the model and scaffold rather than hand-writing, except for data backfill and sequence seeding;
- review the generated SQL — scaffolding sometimes drops and recreates an index needlessly.

Sales has 12 migrations, Inventory 7. Both are applied at startup: Sales inside `IdentitySeeder.SeedIdentityAsync`, Inventory in `RunStartupTasksAsync`. `EnsureCreated()` is never used — it bypasses migrations entirely.

## Reference data

Seeded with `HasData` and stable GUIDs declared in `Persistence/SeedData/ReferenceData/`: 5 colours, 8 sizes, and one `Uncategorized` category.

Those GUIDs appear in exactly one place in the solution. Clients resolve them through `GET /api/common/colors|sizes` and `GET /api/categories` and submit the returned `id` — which is why no seeded GUID is hardcoded in the Angular app.

## Common mistakes

| Mistake | Consequence |
|---|---|
| Unique index without the soft-delete filter | permanently unusable codes, invisible conflicts |
| Editing an applied migration | the next environment diverges |
| Forgetting the model snapshot | the following migration is generated against a stale model |
| `EnsureCreated()` | no migration history at all |
| Provider-specific construct without `IsNpgsql()` | SQLite-backed tests cannot build the schema |
| Renaming an enum member | orphaned string values in existing rows |
| A filter column with no index | a sequential scan on every search |

## Related

- [../tech/database-conventions.md](../tech/database-conventions.md)
- [../tech/database-schema.md](../tech/database-schema.md)
- [../project/backend/migration-rule.md](../project/backend/migration-rule.md)
