# Validation Rules Reference

Every FluentValidation rule that exists today, plus where the corresponding domain guarantee lives.

## Shared rule extensions

| Extension | Rule | File |
|---|---|---|
| `ValidAggregateId()` | `NotEmpty()` on a `Guid` | `Common/Extensions/CommonValidationRules.cs` |
| `ValidExpectedVersion()` | `GreaterThanOrEqualTo(0)` on a `long` | same |
| `ValidCustomerName()` | `NotEmpty().MaximumLength(200)` | `Features/Customers/Validators/CustomerValidationRules.cs` |
| `ValidPhone()` | `NotEmpty()` + 9–15 digits, message `Phone must contain 9 to 15 digits.` | same |
| `HaveUniqueProducts()` | no repeated `ProductVariantId`, message `A product variant can occur only once in an order.` | `Features/Orders/Validators/OrderValidationRules.cs` |

## Sales — Customers

| Validator | Rules |
|---|---|
| `CreateCustomerValidator` | `Name` valid customer name; `Phone` valid phone |
| `UpdateCustomerValidator` | `Id` valid id; + the above |
| `UpdateCustomerStatusValidator` | `Id` valid id; `Status` not empty, ≤ 32 |
| `DeleteCustomerValidator` | `Id` valid id |

## Sales — Orders

| Validator | Rules |
|---|---|
| `CreateOrderValidator` | `CustomerId` valid id; `Lines` not empty; each line via `OrderLineInputValidator`; lines have unique products |
| `ReplaceOrderLinesValidator` | `Id` valid id; `ExpectedVersion` valid; + the line rules above |
| `ConfirmOrderValidator` | `Id` valid id; `ExpectedVersion` valid |
| `CancelOrderValidator` | same |
| `UndoConfirmOrderValidator` | same |
| `OrderLineInputValidator` | `ProductVariantId` valid id; `Quantity > 0`; `DiscountPercent` not null and 0–100 |
| `CancelExpiredPendingOrdersValidator` | `CurrentUtc` not default; `ExpirationMinutes > 0`; `BatchSize > 0` |

## Sales — Products & Categories

| Validator | Rules |
|---|---|
| `CreateProductValidator` | `Name` not empty ≤ 200; `Description` ≤ 1000; `CategoryId` valid id; each variant: `ColorId`/`SizeId` valid id, `Price >= 0`, `Status` not empty ≤ 32 |
| `UpdateProductValidator` | `Id` valid id; + name/description/category/status as above |
| `DeleteProductValidator` | `Id` valid id |
| `AddProductVariantValidator` | `ProductId`/`ColorId`/`SizeId` valid id; `Price >= 0`; `Status` not empty ≤ 32 |
| `UpdateProductVariantValidator` | + `VariantId` valid id |
| `DeactivateProductVariantValidator` | `ProductId`, `VariantId` valid id |
| `DeleteProductVariantValidator` | `ProductId`, `VariantId` valid id |
| `CreateCategoryValidator` | `Name` not empty ≤ 200; `Description` ≤ 1000; `ParentCategoryId` if present must not be `Guid.Empty` |
| `UpdateCategoryValidator` | + `Id` valid id, `Status` not empty ≤ 32 |

## Inventory

| Validator | Rules |
|---|---|
| `AdjustInventoryCommandValidator` | `ProductId` not empty; `Sku` not empty ≤ 64; `Actor` not empty ≤ 128 |
| `ReserveStockCommandValidator` | `EventId`, `OrderId`, `CorrelationId` not empty; `OrderVersion > 0`; `Lines` not empty; product ids unique (`A product can only appear once per reservation request.`); each line: `ProductId` not empty, `Sku` not empty, `Quantity > 0` |
| `ReleaseStockCommandValidator` | `EventId`, `OrderId`, `CorrelationId` not empty; `OrderVersion > 0` |

`DeleteCategoryCommand` has no validator — the handler's `NotFoundException` and dependency check cover it.

## Validation that is not FluentValidation

| Guarantee | Where |
|---|---|
| Status strings parse to a known enum | `Enum.TryParse(..., ignoreCase: true)` in each handler → `DomainException` |
| Phone contains 9–15 digits (authoritative) | `Customer.NormalizePhone` |
| Business codes match `^[A-Z0-9][A-Z0-9_-]*$` | `ProductCodeRules.Normalize` |
| Colour hex matches `^#[0-9A-F]{6}$` | `Color.NormalizeHexCode` |
| Money non-negative and rounded | `Money.Vnd` |
| Order/variant/category/customer state transitions | the aggregates |
| Category parent chain is acyclic | `CategoryCommandSupport.EnsureParentIsValidAsync` |
| Category has no live dependents before delete | `DeleteCategoryHandler` |
| Uniqueness of codes, SKUs, phones, colour/size pairs | filtered unique indexes in Postgres |
| Paging bounds | `Paging.Normalize` (clamps, never rejects) |

Validators improve the error message; the aggregate and the database are the guarantees.

## Related

- [../../project/backend/validation-rule.md](../../project/backend/validation-rule.md)
- [../exception-and-error-catalog.md](../exception-and-error-catalog.md)
