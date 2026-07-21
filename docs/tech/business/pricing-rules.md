# Pricing Rules

## Money

`Sales.Domain.ValueObjects.Money` is the only monetary type.

- Currency is **VND only**. There is no multi-currency support and no currency field.
- Rounded to 0 decimal places with `MidpointRounding.AwayFromZero` on construction.
- Never negative — `Money.Vnd(negative)` throws `DomainException("Money cannot be negative.")`.
- Only `+` is defined. There is deliberately no subtraction or division operator.
- Persisted as `numeric(18,0)` through a `ValueConverter<Money, decimal>` declared in `ProductVariantConfiguration` and `OrderLineConfiguration`.
- Exposed to clients as a plain `decimal`.

## Where price lives

- Price is a **variant** property (`ProductVariant.Price`), not a product property. Different colours/sizes of one product can be priced differently.
- `ProductDto` exposes `MinPrice` / `MaxPrice`, computed from **published** variants only; both are `null` when a product has no published variant (`ProductReadService.MapProduct`).
- Validators require `Price >= 0`.

## Order line pricing

```
LineTotal = Money.Vnd(UnitPrice.Amount * Quantity * (1 - DiscountPercent / 100))
Order.Total = Σ LineTotal
```

- `UnitPrice` is the variant price **snapshotted** when the line was created or last replaced. A later price change never rewrites an existing order.
- `DiscountPercent` is per line, `decimal`, required, inclusive 0–100 (`OrderLine.Validate`, `OrderLineInputValidator`).
- Rounding happens per line, at `Money.Vnd` construction — then the rounded line totals are summed. Totals are not recomputed from an unrounded grand total.
- `Order.Total` and `OrderLine.LineTotal` are computed properties, `Ignore`d by EF, never stored.

## What does not exist

There is no order-level discount, no coupon or promotion engine, no tax calculation, no shipping cost, no currency conversion, and no price-history table. A promotion design exists as a spec only (`docs/superpowers/specs/2026-07-16-sales-product-order-coupon-history-design.md`) and is not implemented — see [../discrepancies.md](../discrepancies.md).

## Code references

`Sales.Domain/ValueObjects/Money.cs`, `Sales.Domain/Entities/{OrderLine,ProductVariant}.cs`, `Sales.Domain/Aggregates/Order.cs`, `Sales.Infrastructure/Persistence/ReadServices/ProductReadService.cs`.

## Related

- [order-lifecycle.md](order-lifecycle.md)
- [catalog-rules.md](catalog-rules.md)
