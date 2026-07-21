# Cấu trúc DDD của solution

Tài liệu tra cứu nhanh: solution áp dụng tactical pattern DDD nào, ở đâu, và lệch chỗ nào có chủ đích. Chi tiết layer/thư mục xem [02-solution-structure.md](02-solution-structure.md), quy tắc bắt buộc xem [../project/backend/](../project/backend/).

## Tóm tắt nhanh

| Pattern DDD | Sales | Inventory | Vị trí code |
|---|:---:|:---:|---|
| Aggregate Root giàu hành vi (rich model) | ✅ | ✅ | `*.Domain/Aggregates/` |
| Entity con chỉ sửa qua aggregate root | ✅ | ✅ | `*.Domain/Entities/` |
| Value Object bất biến | ✅ | ✅ | `*.Domain/ValueObjects/` |
| Domain Event phát sinh từ aggregate | ✅ | ❌ | xem [mục 5](#5-domain-event--outbox--integration-event) |
| Domain-specific exception (`DomainException`) | ✅ | ❌ (dùng `InvalidOperationException`) | |
| Repository theo aggregate, ẩn ORM khỏi Domain | ✅ | ✅ | `*.Domain/Repositories/` + Infrastructure implementation |
| Application layer mỏng, không chứa rule | ✅ | ✅ | `*.Application/Commands/`, `*.Application/Queries/` |
| Bounded context tách biệt, giao tiếp qua event | ✅ | ✅ | `BuildingBlocks.Contracts` |

❌ còn lại ở Inventory là **lệch có chủ đích**, lý do ở [mục 6](#6-lệch-khỏi-ddd-sách-vở--có-chủ-đích).

## 1. Bounded context

| Context | Vai trò | Domain model |
|---|---|---|
| **Sales** | Customer, Product, Order + vòng đời đơn hàng | Rich — đầy đủ aggregate/event/invariant |
| **Inventory** | Tồn kho + giữ chỗ (reservation) | Rich nhưng domain-event pipeline còn đơn giản hoá (xem mục 6) |
| **AuditLog** | Ghi log thay đổi từ integration event | Không có — đúng bản chất (sink, không nghiệp vụ) |

3 context có database riêng, **không** project reference chéo — chỉ giao tiếp qua integration event (Kafka), enforce bằng architecture test (mục 7).

## 2. Building block dùng chung

```
BuildingBlocks.Domain/
├── Entity<TId>          → có Id, không version/event
├── AggregateRoot<TId>   → kế thừa Entity, thêm Version + buffer domain event
├── IDomainEvent          → marker, chỉ có OccurredAt
└── DomainException       → exception cho vi phạm invariant
```

`AggregateRoot<TId>` ([source](../../src/Shared/BuildingBlocks.Domain/Abstractions/AggregateRoot.cs)) gói 3 khái niệm DDD cốt lõi:

| Method/Property | Vai trò DDD |
|---|---|
| `Version` (long), `Touch()` | Optimistic concurrency — phát hiện 2 request sửa cùng lúc |
| `Raise(event)` — **protected** | Domain event luôn phát sinh từ *bên trong* aggregate, không set từ ngoài |
| `DomainEvents`, `ClearDomainEvents()` | Buffer trong bộ nhớ, Infrastructure đọc rồi xoá sau khi publish |

## 3. Aggregate Root

### Quy tắc chung (áp dụng cho `Order`, `Product`, `Customer`, `Reservation`)

```csharp
// ✅ Cách duy nhất để tạo — không có setter public, không new() từ ngoài
var product = Product.Create(sku, name, price);   // tự validate, tự raise ProductCreatedDomainEvent

// ❌ Không thể làm — sẽ không compile
product.Name = "x";          // Name là { get; private set; }
product.IsActive = true;     // idem
new Product();                // constructor private
```

| Aggregate | Invariant chính | File |
|---|---|---|
| `Order` | ≥1 line, không trùng product, state machine hợp lệ | [Order.cs](../../src/Services/Sales/Sales.Domain/Aggregates/Order.cs) |
| `Product` | SKU/tên không rỗng, giá ≥ 0, không sửa khi đã xoá | [Product.cs](../../src/Services/Sales/Sales.Domain/Aggregates/Product.cs) |
| `Customer` | Tên không rỗng, phone 9–15 số | [Customer.cs](../../src/Services/Sales/Sales.Domain/Aggregates/Customer.cs) |
| `Reservation` | ≥1 line, không trùng product, state `Active ⇄ Released` | [Reservation.cs](../../src/Services/Inventory/Inventory.Domain/Aggregates/Reservation.cs) |
| `InventoryItem` | `Available` không bao giờ âm | [InventoryItem.cs](../../src/Services/Inventory/Inventory.Domain/Entities/InventoryItem.cs) |

### State machine của `Order`

```
Draft ──RequestConfirmation()──▶ PendingInventory ──MarkReserved()──▶ Confirmed
  │                                     │                                │
  │                              RejectInventory()               UndoConfirmed()
  │                                     ▼                                │
  └──────────Cancel()──────▶  InventoryRejected / Cancelled  ◀───────────┘
```

Mỗi transition tự `Raise()` đúng 1 event tương ứng, và ném `DomainException` nếu gọi sai thứ tự (vd `MarkReserved()` khi đang `Draft`).

### Update thông minh — không tạo event thừa

```csharp
// Product.Update() / Customer.Update() đều làm vậy:
var oldName = Name;
Rename(name);
if (oldName == Name && ...) return;   // không đổi gì → không Touch(), không Raise()
Touch();
Raise(new ProductUpdatedDomainEvent(...));
```

## 4. Entity con — chỉ truy cập qua Aggregate Root

`OrderLine` (con của `Order`) và `ReservationLine` (con của `Reservation`) đều kế thừa `Entity<TId>`, **không** phải `AggregateRoot<TId>`.

```csharp
// Sales.Domain/Entities/OrderLine.cs
internal static OrderLine Create(...)     // internal → chỉ Order (cùng assembly) gọi được
internal void ReplaceWith(...)            // Application/Infrastructure không tự sửa OrderLine
```

→ Không có `IOrderLineRepository` — entity con không có vòng đời độc lập, không cần repository riêng.

## 5. Domain Event → Outbox → Integration Event

```
1. Order.RequestConfirmation()
   └─▶ Raise(new OrderConfirmationRequestedDomainEvent(...))     [chỉ buffer trong bộ nhớ]

2. SalesDbContext.SaveChangesAsync() (override, trước khi lưu DB)
   └─▶ DomainEventMapper.Map(...)   domain event → (topic, integration event)
   └─▶ OutboxMessages.Add(...)      cùng transaction với thay đổi aggregate — atomic

3. base.SaveChangesAsync()  →  aggregate.ClearDomainEvents()

4. SalesOutboxPublisher (BackgroundService)  →  publish thật lên Kafka

5. Inventory/AuditLog consume qua Inbox pattern (idempotent theo eventId)
```

Điểm mấu chốt: **domain event (nội bộ, `Sales.Domain`) ≠ integration event (`BuildingBlocks.Contracts`)**. Domain không biết Kafka topic; việc map là trách nhiệm riêng của `DomainEventMapper` (Infrastructure).

| Ở Inventory | Khác gì Sales |
|---|---|
| Chưa dùng domain event nội bộ cho mọi thay đổi | Inventory command handlers enqueue integration event qua `IInventoryEventOutbox`; Kafka adapter chỉ dispatch command qua MediatR |

## 6. Lệch khỏi DDD "sách vở" — có chủ đích

| Điểm lệch | Lý do (từ [architecture-checklist.md](../tech/architecture-checklist.md)) |
|---|---|
| Inventory chưa raise domain event qua `AggregateRoot.Raise()` cho mọi flow | Hiện command handlers enqueue integration event qua `IInventoryEventOutbox`; đổi sang domain-event pipeline riêng là refactor khác |
| Inventory dùng `InvalidOperationException` thay vì `DomainException` | Test hiện có assert đúng `InvalidOperationException` — đổi sẽ phá behavior |
| Inventory read model dùng read service riêng | Query handler gọi `IInventoryReadService`; read side không đi qua repository command-side |
| `Inventory.Api` dispatch qua MediatR | Controller gọi `ISender.Send(...)`; Kafka adapter cũng map integration event sang command rồi dispatch qua MediatR |

Đây là nợ kỹ thuật **được ghi nhận rõ ràng**, không phải bị bỏ sót — nên cân nhắc refactor nếu nghiệp vụ Inventory phức tạp lên.

## 7. Application layer — mỏng, không chứa business rule

```csharp
// ✅ Thực tế — CreateOrderHandler.cs
var order = Order.Create(customerSnapshot, lines);  // rule nằm ở Order.Create
await orders.AddAsync(order, ct);
await uow.SaveChangesAsync(ct);

// ❌ Anemic model — KHÔNG có trong solution, chỉ để đối chiếu
if (lines.Count == 0) throw ...;                     // rule lẽ ra thuộc Order lại nằm ở Handler
if (lines.GroupBy(x => x.ProductId).Any(g => g.Count() > 1)) throw ...;
var order = new Order { Status = "Draft", Lines = lines };  // tạo trực tiếp, không qua factory
```

Handler chỉ: load aggregate → gọi domain behavior → enqueue outbox nếu cần → commit qua Unit of Work/transaction behavior. Không `if (order.Status == ...)` nào rò ra Application trong Sales; Inventory có orchestration kiểm tra stale/idempotency vì phải phối hợp `InventoryItem` và `Reservation` nhưng invariant vẫn nằm trong aggregate/entity.

## 8. Repository & Value Object — bảng tra nhanh

**Repository** (`Sales.Domain/Repositories/`, `Inventory.Domain/Repositories/`):

```csharp
public interface IRepository<T> where T : AggregateRoot<Guid>   // ràng buộc compile-time
```

→ Không tạo repository cho `OrderLine`/`ReservationLine` (entity con không có vòng đời độc lập). `IOrderRepository`/`IProductRepository` chỉ thêm method khi có query đặc thù (`GetWithLinesAsync`, `GetBySkuAsync`) — `Customer` dùng thẳng `IRepository<Customer>`, không tạo interface thừa. Inventory có `IInventoryRepository` và `IReservationRepository` vì command handlers cần load aggregate theo `ProductId`/`OrderId`.

**Value Object** (`Sales.Domain/ValueObjects/`):

| Type | Dùng để |
|---|---|
| `Money` | Số tiền bất biến, không âm, so sánh theo giá trị (`record struct`) |
| `ProductSnapshot` / `CustomerSnapshot` | Bản chụp dữ liệu tại 1 thời điểm — `OrderLine` không giữ tham chiếu sống tới `Product` |
| `OrderLineItem` | Input value object cho `Order.Create`/`ReplaceLines`, thay tuple |

## 9. Enforce tự động

`Sales.Architecture.Tests` (NetArchTest) chặn vi phạm dependency âm thầm theo thời gian:

- `Sales_domain_does_not_depend_on_outer_layers_or_other_services`
- `Inventory_domain_is_isolated`
- `BuildingBlocks_domain_is_framework_independent`
- `Sales_application_does_not_depend_on_infrastructure_or_other_services`

`Sales.Domain.Tests` test invariant trực tiếp trên aggregate — không mock, không cần DB.

## 10. Lỗi nhỏ phát hiện được (không phải vi phạm DDD)

- `Sales.Domain/Events/Orders/OrderCancelledDomainEvent.cs` — tên file không khớp type bên trong (`OrderUndoComfirmedDomainEvent`).
- `Reservation.cs`, `ReservationLine.cs` (Inventory) — XML doc comment thiếu tag mở `<summary>` ở property đầu tiên.
