# Stock Rules

Quick-reference companion to [inventory-lifecycle.md](inventory-lifecycle.md).

## Invariants

| Rule | Enforced by |
|---|---|
| `Available` is never negative | `InventoryItem.Adjust`, `.Reserve` |
| `Reserved` is never negative | `InventoryItem.Release` |
| Reserve quantity must be positive and ≤ `Available` | `InventoryItem.Reserve` |
| Release quantity must be positive and ≤ `Reserved` | `InventoryItem.Release` |
| Initial stock cannot be negative | `InventoryItem.Create` |
| Every mutation bumps `Version` | `InventoryItem` |
| A reservation needs at least one line, all quantities positive, no repeated product | `Reservation.SetLines` |
| A reservation line's product can never be changed | `ReservationLine.ReplaceWith` |
| One reservation per order | unique index on `Reservation.OrderId` |

## All-or-nothing reservation

A reservation request either reserves every line or reserves nothing. `ReserveStockCommandHandler.FindRejectedLine` scans all requested lines first; if any variant has no inventory item or `Available < Quantity`, the whole request is rejected with `Insufficient stock for {sku}.` and no stock moves. There is no partial fulfilment and no backorder concept.

## Re-confirmation with changed lines

When a confirmation arrives for an order that already holds an `Active` reservation, the handler computes per-line deltas against the existing reservation:

- `delta > 0` → reserve the difference
- `delta < 0` → release the difference
- line removed → release its full quantity
- availability check uses `item.Available + currentlyReserved >= requested`, so an order can keep stock it already holds

## Stock accounting summary

| Operation | Available | Reserved |
|---|---|---|
| `Adjust(+n)` | `+n` | — |
| `Adjust(-n)` | `-n` | — |
| `Reserve(n)` | `-n` | `+n` |
| `Release(n)` | `+n` | `-n` |

Sales never writes stock. Stock changes only through the Inventory API adjustment endpoint or through consumed Sales order events.

## No auto-provisioning from the catalog

Creating a product variant in Sales does **not** create an `InventoryItem`. An item appears the first time stock is adjusted for that variant id. A confirmation for a variant with no inventory item is rejected as insufficient stock. This is intentional today and worth knowing when seeding demo data — see [../discrepancies.md](../discrepancies.md).

## Concurrency

- `InventoryItem.Version` is an EF concurrency token.
- Every Inventory command runs in a `Serializable` transaction, so two concurrent reservations for the same item cannot both read the same `Available`. The loser gets a serialization failure, surfaced as `409 concurrency_conflict` with `retryable=True`.
- Two concurrent adjustments for a not-yet-existing item conflict on the primary key and produce `409 unique_violation`.

## Related

- [inventory-lifecycle.md](inventory-lifecycle.md)
- [../concurrency-and-idempotency.md](../concurrency-and-idempotency.md)
