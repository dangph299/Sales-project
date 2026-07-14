# Inventory CQRS/MediatR refactor audit

Tài liệu này ghi lại audit và mapping refactor Inventory theo CQRS/MediatR.

## Shared components reused

- `BuildingBlocks.Application.IUnitOfWork`: dùng trực tiếp trong Inventory command handlers, không tạo `IInventoryUnitOfWork`.
- `BuildingBlocks.Application.ICommand<T>` và `IQuery<T>`: dùng cho command/query Inventory.
- `BuildingBlocks.Application` pipeline behaviors: validation, logging, performance, error logging qua `AddApplicationBuildingBlocks()`.
- `BuildingBlocks.Application.IClock`: dùng lại ở Infrastructure inbox.
- `BuildingBlocks.Infrastructure.OutboxPublisherService`, `OutboxMessage`, `KafkaOutboxPublisher`: tiếp tục dùng cho outbox publish/retry/dead-letter.
- `BuildingBlocks.Infrastructure.IIntegrationEventProcessor`: Kafka adapter vẫn dùng interface shared hiện có.
- `BuildingBlocks.Contracts`: dùng integration events và Kafka topic/group constants.

## Inventory-specific abstractions created

| Abstraction | Reason |
|---|---|
| `IInventoryReadService` | Query projection đặc thù Inventory cho inventory snapshot và reservation snapshot. |
| `IInventoryEventOutbox` | Mapping event đặc thù Inventory sang `StockReserved`, `StockRejected`, `StockReleased`, `InventoryAudit`. |
| `IInventoryInbox` | Inbox idempotency của Inventory; hiện BuildingBlocks chưa có `IIdempotencyService` chung. |
| `IInventoryMetrics` | Metrics nghiệp vụ Inventory: reservation reserved/rejected và inbox processed/duplicate. |
| `IInventoryTransactionManager` + `IInventoryTransaction` | Mở transaction serializable quanh event-processing use case; không bọc lại `IUnitOfWork`. |
| `IInventoryRepository`, `IReservationRepository` | Repository aggregate boundary đặt trong `Inventory.Domain`. |

## Mapping

| Current class/method | Current responsibility | Problem | Target layer | Target command/query/domain method | Reused shared component | New component required | Reason |
|---|---|---|---|---|---|---|---|
| `InventoryService.GetAsync` | Đọc snapshot tồn kho từ EF | API gọi Infrastructure service trực tiếp | Application query + Infrastructure read adapter | `GetInventoryByProductQuery` / `GetInventoryByProductQueryHandler` | `IQuery<T>`, MediatR | `IInventoryReadService` | Query projection đặc thù Inventory. |
| `InventoryService.GetReservationAsync` | Đọc reservation snapshot từ EF | Query trộn trong Infrastructure service | Application query + Infrastructure read adapter | `GetReservationByOrderQuery` / handler | `IQuery<T>`, MediatR | `IInventoryReadService` | Reservation read model đặc thù Inventory. |
| `InventoryService.AdjustAsync` | Tạo/cập nhật item, enqueue audit, save | Use-case orchestration nằm trong Infrastructure | Application command | `AdjustInventoryCommand` / handler, `InventoryItem.Create`, `InventoryItem.Adjust` | `ICommand<T>`, `IUnitOfWork`, ValidationBehavior | `IInventoryEventOutbox`, `IInventoryRepository` | Business flow đi qua Application, persistence/event mapping ở Infrastructure. |
| `InventoryOrderEventProcessor.ProcessAsync` | Inbox transaction, dispatch event type, reserve/release | Infrastructure chứa workflow | Infrastructure adapter + Application commands | `InventoryIntegrationEventProcessor` maps to commands | `IIntegrationEventProcessor`, MediatR | None beyond command types | Kafka layer chỉ adapter. |
| `InventoryOrderEventProcessor.Reserve` | Reserve stock, create/reactivate reservation, enqueue reply | Business workflow trong Infrastructure | Application command handler + Domain methods | `ReserveStockCommandHandler`, `InventoryItem.Reserve`, `Reservation.Create/Reactivate/ReplaceActive` | `ICommand<T>`, `IUnitOfWork` | `IInventoryInbox`, `IInventoryTransactionManager`, repositories, outbox port | Handler điều phối use case; invariant vẫn trong Domain. |
| `InventoryOrderEventProcessor.Release` | Release stock, mark reservation released, enqueue reply | Business workflow trong Infrastructure | Application command handler + Domain methods | `ReleaseStockCommandHandler`, `InventoryItem.Release`, `Reservation.Release` | `ICommand<T>`, `IUnitOfWork` | Same Inventory-specific ports above | Tách Kafka adapter khỏi workflow. |
| `InventoryController` | Gọi `IInventoryService` | API đi qua Infrastructure service | API adapter | Sends `GetInventoryByProductQuery`, `GetReservationByOrderQuery`, `AdjustInventoryCommand` | MediatR `ISender` | None | Controller chỉ nhận HTTP và dispatch use case. |

## Duplicate or unused abstractions removed

- Removed `IInventoryService`: API no longer calls an Infrastructure-backed application service.
- Removed `InventoryService`: replaced by command/query handlers and read adapter.
- Removed `InventoryOrderEventProcessor`: replaced by `InventoryIntegrationEventProcessor` adapter plus command handlers.
- Removed `IInventoryUnitOfWork` after review: handlers now inject shared `IUnitOfWork` directly.
- Removed query types that were created from a template but not backed by existing use cases: `GetAvailableStock`, `CheckStockAvailability`, `SearchInventory`.
- Removed empty `Inventory.Application/Interfaces` folder.

## Domain behavior added

- `Reservation.ReplaceActive(...)`: updates active reservation lines for a newer confirmation event. This fixes the reviewed bug where a newer confirmation could arrive before an older release and leave stock/reservation lines stale.

## Remaining risks

- `IInventoryInbox` and transaction handling are still service-specific because no shared idempotency/transaction abstraction exists yet. Move to BuildingBlocks only if another service needs the same stable contract.
- No database schema migration was added; the refactor changes orchestration only.
