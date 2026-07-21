# Catalog Rules (Category, Product, Variant)

The Sales catalog has three levels: `Category` → `Product` → `ProductVariant`, plus two seeded reference entities, `Color` and `Size`.

## Codes

All business codes are normalized by `ProductCodeRules.Normalize`: trimmed, upper-cased, must match `^[A-Z0-9][A-Z0-9_-]*$`.

| Code | Sequence | Prefix | Allocated by |
|---|---|---|---|
| `CategoryCode` | `category_code_seq` | `CAT` | `CategoryCodeGenerator` |
| `ProductCode` | `product_code_seq` | `PRD` | `ProductCodeGenerator` |
| `CustomerCode` | `customer_code_seq` | `CUS` | `CustomerCodeGenerator` |

Codes are backend-assigned and never accepted from a client. They are unique and monotonic but **not gap-free** — a sequence does not roll back, so a number consumed by a failed create is skipped. Allocation happens *after* validation so a rejected request does not burn a number (`CreateCategoryHandler`, `CreateProductHandler`).

## Category

Status: `Draft` → `Published` → `Archived` (`ECategoryStatus`).

| Rule | Where |
|---|---|
| Name required, trimmed | `Category.ChangeDetails` |
| A category cannot be its own parent | `Category.ChangeDetails` |
| The parent chain must not be circular | `CategoryCommandSupport.EnsureParentIsValidAsync` |
| Only `Draft` can be published | `Category.Publish` |
| Only `Published` can be archived | `Category.Archive` |
| Deleting a `Published` category archives it as well | `Category.Delete` |
| A category cannot be deleted while a live child category or product references it | `DeleteCategoryHandler` + `ICategoryReadService.HasDependentsAsync` |
| Deleted categories cannot be changed | `Category.EnsureNotDeleted` |
| Only a `Published` category may be assigned to a product | `CreateProductHandler`, `UpdateProductHandler` |

Uniqueness (excluding soft-deleted rows): `CategoryCode`; `(Name, ParentCategoryId)`; `Name` where `ParentCategoryId IS NULL`.

Seed: `Uncategorized` (`CAT001`, `30000000-0000-0000-0000-000000000001`), published, the default for products migrated from the legacy schema.

## Product

Status: `Draft` → `Published` → `Discontinued` (`EProductStatus`). A discontinued product can be re-published.

| Rule | Where |
|---|---|
| Name required; category required and non-empty | `Product.ChangeDetails` |
| Only `Draft` or `Discontinued` products can be published | `Product.Publish` |
| Only `Published` products can be discontinued | `Product.Discontinue` |
| Deleted products cannot be changed | `Product.EnsureNotDeleted` |
| `IsActive` = not deleted **and** has at least one non-deleted `Published` variant | `Product.IsActive` |
| `Sku` = the lowest-SKU published variant's SKU, else `ProductCode` | `Product.Sku` |
| Creating a product with a `Published` variant publishes the product | `CreateProductHandler` |

`ProductCode` is unique excluding soft-deleted rows. `Name` is indexed with `gin_trgm_ops` for `ILIKE` search.

## Product variant

Status: `Draft` → `Published` → `Discontinued`, and `Discontinued` → `Published` (`EProductVariantStatus`).

| Rule | Where |
|---|---|
| SKU = `ProductCode-ColorCode-SizeCode` | `ProductCodeRules.BuildSku` |
| A `(ProductId, ColorId, SizeId)` triple can exist once per product | `Product.EnsureVariantDoesNotExist` + unique index |
| A new variant cannot be created as `Discontinued` | `Product.EnsureCanCreateVariant` |
| Allowed transitions only: Draft→Published, Published→Discontinued, Discontinued→Published | `ProductVariant.EnsureCanChangeTo` |
| Only `Draft` or `Discontinued` variants can be deleted | `ProductVariant.Delete` — a published variant must be discontinued first |
| Deleted variants cannot be changed | `ProductVariant.EnsureNotDeleted` |
| Price is `Money.Vnd` — non-negative, rounded to whole VND | `ProductVariant.Create/Update` |

Uniqueness excluding soft-deleted rows: `Sku`; `(ProductId, ColorId, SizeId)`. Deleting a variant therefore frees its colour/size pair and its SKU for reuse.

Variants are created and mutated only through `Product` (`AddVariant`, `UpdateVariant`, `PublishVariant`, `DiscontinueVariant`, `DeleteVariant`), which calls `Touch()` on the product so the product's ETag changes too.

## Orderability

| Situation | Can be ordered? |
|---|---|
| Variant `Published` | yes |
| Variant `Discontinued` | yes — **sell-through**, flagged `IsSellThroughDiscontinued` on the line |
| Variant `Draft` | no |
| Variant deleted / product deleted | no |

`OrderCommandSupport.IsOrderableVariant` is the single definition. An order line created from a discontinued variant carries `IsSellThroughDiscontinued = true`, which blocks `Order.UndoConfirmed`.

## Reference data

`Color` (5 seeded: BLK, WHT, RED, BLU, GRN with hex codes) and `Size` (8 seeded: XXS…XXXL with sort orders 10–80) are immutable at runtime. Their GUIDs live in `Persistence/SeedData/ReferenceData/`. Clients resolve them through `GET /api/common/colors|sizes` and submit the returned `id`.

- Colour hex must match `^#[0-9A-F]{6}$` (`Color.NormalizeHexCode`).
- `Color.ColorCode` and `Color.Name` are unique; `Size.Code` and `Size.SortOrder` are unique.

## Code references

`Sales.Domain/Aggregates/{Category,Product}.cs`, `Sales.Domain/Entities/{ProductVariant,Color,Size}.cs`, `Sales.Domain/Services/ProductCodeRules.cs`, `Sales.Application/Features/Products/`, `Sales.Infrastructure/Persistence/Configurations/`.

## Related

- [pricing-rules.md](pricing-rules.md)
- [order-lifecycle.md](order-lifecycle.md)
- [validation-rules.md](validation-rules.md)
