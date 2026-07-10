# Sales Management DDD - Tài liệu trình bày project

Tài liệu này dùng để trình bày lại bài thực hành Sales Management theo DDD: yêu cầu là gì, project đang dùng công nghệ nào, từng phần nằm ở đâu trong source code, và flow hoạt động ra sao.

## 1. Mục tiêu bài thực hành

Project mô phỏng hệ thống quản lý bán hàng gồm:

- Danh mục sản phẩm: tạo/sửa/xem/tìm sản phẩm theo tên.
- Khách hàng: tạo/sửa/xem/tìm theo tên, số điện thoại đầu số hoặc đuôi số.
- Đơn hàng: lưu snapshot khách hàng/sản phẩm, tổng số lượng, tổng tiền, chiết khấu từng dòng.
- Xử lý 2 người cùng sửa đơn hàng bằng optimistic concurrency với `ETag` / `If-Match`.
- Inventory service riêng để quản lý tồn kho và reservation.
- AuditLog lưu MongoDB, nhận event qua Kafka.
- Messaging theo at-least-once delivery, có Outbox/Inbox để không miss event.
- Observability: log Seq, trace/metric OpenTelemetry, Elastic APM/Kibana.
- Docker Compose chạy đầy đủ hạ tầng local.

## 2. Kiến trúc tổng thể

Project chia thành 3 process chính:

| Process | Vai trò | Project |
|---|---|---|
| Sales API | Modular monolith cho Product, Customer, Order, Identity | `src/Services/Sales/Sales.Api` |
| Inventory API | Service riêng quản lý stock/reservation | `src/Services/Inventory/Inventory.Api` |
| AuditLog Worker | Worker nhận Kafka event và lưu MongoDB | `src/Services/AuditLog/AuditLog.Worker` |

Shared contracts nằm ở:

```text
src/Shared/BuildingBlocks.Contracts/
```

Hạ tầng local nằm ở:

```text
docker/docker-compose.yml
docker/otel-collector-config.yaml
docker/apm-server.yml
```

Luồng dependency theo Clean Architecture:

```text
Api / Worker
    ↓
Infrastructure
    ↓
Application
    ↓
Domain
```

Rule quan trọng:

- `Domain` không biết EF Core, Kafka, Redis, HTTP, MediatR.
- `Application` chứa use case, command/query, DTO, port/interface.
- `Infrastructure` implement persistence, repository, Kafka, Redis, Hangfire.
- `Api` chỉ là transport HTTP, authorization, controller, middleware.

## 3. Cấu trúc thư mục và vai trò

### Sales.Domain

Vị trí:

```text
src/Services/Sales/Sales.Domain
```

Dùng cho domain model, invariant, business rule.

| Folder | Vai trò | Ví dụ |
|---|---|---|
| `Aggregates` | Aggregate root và entity nằm trong aggregate | `Order`, `Product`, `Customer`, `OrderLine` |
| `ValueObjects` | Object không có identity, so sánh theo giá trị | `Money`, `CustomerSnapshot`, `ProductSnapshot` |
| `Events` | Domain event mô tả việc đã xảy ra | `OrderConfirmationRequestedDomainEvent` |
| `Exceptions` | Lỗi nghiệp vụ domain | `DomainException` |
| `Repositories` | Interface repository command-side | `IOrderRepository` |

Ví dụ `Order` là aggregate root, quản lý nhất quán cho các `OrderLine`:

- Order phải có ít nhất 1 dòng.
- Một sản phẩm chỉ xuất hiện 1 lần trong đơn.
- Chỉ đơn `Draft` được sửa.
- Confirm chuyển `Draft -> PendingInventory`.
- Inventory trả kết quả rồi Sales chuyển sang `Confirmed` hoặc `InventoryRejected`.

### Sales.Application

Vị trí:

```text
src/Services/Sales/Sales.Application
```

Dùng cho CQRS và use case.

| Folder | Vai trò | Ví dụ |
|---|---|---|
| `Commands` | Use case ghi dữ liệu qua aggregate/repository/UoW | `CreateProduct`, `ReplaceOrderLines`, `ConfirmOrder` |
| `Queries` | Use case đọc dữ liệu qua read service | `SearchProducts`, `SearchOrders` |
| `DTOs` | Response model/API model | `ProductDto`, `OrderDto`, `PagedResult<T>` |
| `Interfaces` | Port để Infrastructure implement | `IProductReadService`, `IUnitOfWork`, `IProductCache` |
| `Services` | Application exception/orchestration | `NotFoundException`, `ConflictException` |

Command không map thẳng vào aggregate. Command handler load aggregate, gọi behavior domain, rồi commit Unit of Work.

Ví dụ:

```text
HTTP PUT /api/orders/{id}/lines
    -> OrdersController
    -> ReplaceOrderLines command
    -> OrderCommandHandler
    -> IOrderRepository.GetAsync
    -> Order.ReplaceLines
    -> IUnitOfWork.SaveChangesAsync
```

### Sales.Infrastructure

Vị trí:

```text
src/Services/Sales/Sales.Infrastructure
```

Dùng để nối Application với database, Redis, Kafka, Hangfire.

| Folder | Vai trò | Ví dụ |
|---|---|---|
| `Persistence` | EF Core DbContext, migration, read service | `SalesDbContext`, `SalesReadServices` |
| `Repositories` | Repository implementation | `OrderRepository` |
| `Kafka` | Outbox publisher, Inbox consumer, map domain event sang integration event | `SalesOutboxPublisher`, `DomainEventMapper` |
| `Hangfire` | Job cleanup/replay | `MaintenanceJobs` |
| `ExternalServices` | Redis cache, execution context | `ProductCache`, `HttpExecutionContext` |
| `Observability` | Custom metrics | `SalesMetrics` |

### Sales.Api

Vị trí:

```text
src/Services/Sales/Sales.Api
```

Project này dùng MVC Controllers, không dùng Minimal API endpoints cho business APIs.

| Folder | Vai trò | Ví dụ |
|---|---|---|
| `Controllers` | REST API, authorization, ETag, MediatR dispatch | `ProductsController`, `OrdersController` |
| `Middleware` | Exception handler, RFC7807 ProblemDetails | `ExceptionHandlingMiddleware` |
| `Filters` | Filter cho Hangfire dashboard | `LocalDashboardAuthorizationFilter` |
| `Extensions` | Bootstrap/seed identity | `IdentitySeeder` |

Coding rule hiện tại:

- Dùng controller truyền thống.
- Dùng constructor injection truyền thống.
- Không dùng C# primary constructor trong controller/API-facing class.

Ví dụ:

```csharp
private readonly ISender _sender;

public OrdersController(ISender sender)
{
    _sender = sender;
}
```

## 4. Mapping yêu cầu bài với implementation

| Yêu cầu | Đang dùng gì? | Vị trí |
|---|---|---|
| Product tạo/sửa/xem/search tên | Controller + MediatR + Repository + EF query + Redis cache | `ProductsController`, `Products.cs`, `ProductReadService`, `ProductCache` |
| Customer tạo/sửa/xem/search phone đầu/đuôi/tên | Phone normalize, `Phone`, `ReversedPhone`, EF query | `Customer.cs`, `CustomerReadService` |
| Order tổng SL/tổng tiền/chiết khấu | Aggregate `Order`, `OrderLine`, `Money` | `Order.cs`, `Money.cs` |
| Search order theo ngày/tên/SĐT | Query projection `AsNoTracking`, index ngày/tên/SĐT | `OrderReadService`, `SalesDbContext` |
| 2 người cùng sửa order | Optimistic concurrency bằng `Version`, `ETag`, `If-Match` | `OrdersController`, `ControllerEtagExtensions`, `OrderCommandHandler` |
| AuditLog MongoDB qua Kafka | Kafka consumer lưu Mongo, unique `eventId` | `AuditConsumer.cs` |
| Inventory service riêng | Inventory Domain/Application/Infrastructure/API riêng, Postgres riêng | `src/Services/Inventory` |
| Không miss event | Transactional Outbox/Inbox, retry, backoff, DLQ, replay | `SalesOutboxPublisher`, `InventoryOutboxPublisher`, `MaintenanceJobs` |
| CQRS | Command/query tách riêng, MediatR | `Sales.Application/Commands`, `Sales.Application/Queries` |
| Factory Method | Static factory tạo aggregate/value object | `Product.Create`, `Customer.Create`, `Order.Create`, `Money.Vnd` |
| Repository | Interface ở Domain, implement ở Infrastructure | `Repositories.cs` |
| Unit of Work | `SalesDbContext` implement `IUnitOfWork` | `SalesDbContext.cs` |
| Mapster | Mapping domain sang DTO | `DTOs/Models.cs` |
| Redis cache | Cache-aside cho product detail, Redis lock cho cleanup job | `ProductCache.cs`, `MaintenanceJobs.cs` |
| Hangfire | Scheduled cleanup/replay job | `Program.cs`, `MaintenanceJobs.cs` |
| KafkaFlow | Producer/consumer Kafka | `DependencyInjection.cs`, Kafka folder |
| Postgres/Mongo | Postgres cho Sales/Inventory/Hangfire, Mongo cho Audit | `docker-compose.yml` |
| Monitoring | Seq + OTel + Elastic APM/Kibana + custom metrics | `Program.cs`, `SalesMetrics`, `InventoryMetrics`, `docs/observability.md` |

## 5. Product module

Yêu cầu:

- Lưu SKU, tên, giá, trạng thái active.
- Tìm theo tên sản phẩm.
- Cache Redis.
- Không hard-delete product trong MVP vì product có thể đã xuất hiện trên Order snapshot.

Đang dùng:

- Aggregate: `Product`
- Command:
  - `CreateProduct`
  - `UpdateProduct`
- Query:
  - `GetProduct`
  - `SearchProducts`
- Controller:
  - `ProductsController`
- Cache:
  - `ProductCache`

Flow tạo product:

```text
POST /api/products
    -> ProductsController.Create
    -> CreateProduct command
    -> CreateProductHandler
    -> Product.Create
    -> IProductRepository.AddAsync
    -> IUnitOfWork.SaveChangesAsync
    -> Redis cache set cho product vừa tạo
```

Ví dụ API:

Login lấy JWT trước:

```http
POST /api/auth/login
Content-Type: application/json

{
  "userName": "admin",
  "password": "Admin123!"
}
```

Tạo product, cần role `Admin`:

```http
POST /api/products
Authorization: Bearer <token>
Content-Type: application/json

{
  "sku": "SKU-001",
  "name": "Keyboard",
  "price": 250000
}
```

Update product, cần role `Admin`. API update product hiện không dùng `If-Match`; concurrency chính của bài đang áp dụng cho Order.

```http
PUT /api/products/{productId}
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Keyboard Pro",
  "price": 300000,
  "isActive": true
}
```

Search:

```http
GET /api/products?name=key&page=1&pageSize=20
Authorization: Bearer <token>
```

Get detail có trả `ETag` theo `Version`, phục vụ quan sát version/cache:

```http
GET /api/products/{productId}
Authorization: Bearer <token>

HTTP/1.1 200 OK
ETag: "1"
```

Database index:

- `Sku` unique index.
- `Name` dùng PostgreSQL `pg_trgm` GIN index để search gần đúng/case-insensitive.

## 6. Customer module

Yêu cầu:

- Lưu tên khách hàng.
- Lưu phone đã normalize chỉ còn chữ số.
- Search theo tên, phone prefix, phone suffix.
- Không hard-delete customer trong MVP vì customer có thể đã xuất hiện trên Order snapshot.

Đang dùng:

- Aggregate: `Customer`
- Phone normalize trong domain.
- `ReversedPhone` để search suffix hiệu quả.
- Query trong `CustomerReadService`.

Ví dụ:

```http
POST /api/customers
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Nguyen Van A",
  "phone": "+84 901 234 567"
}
```

Update customer. API update customer hiện không dùng `If-Match`; phần chống 2 người cùng sửa tập trung ở Order.

```http
PUT /api/customers/{customerId}
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Nguyen Van B",
  "phone": "+84 902 222 333"
}
```

Phone được lưu thành:

```text
84901234567
```

Search prefix:

```http
GET /api/customers?phone=8490&phoneMatch=prefix
Authorization: Bearer <token>
```

Search suffix:

```http
GET /api/customers?phone=4567&phoneMatch=suffix
Authorization: Bearer <token>
```

Cách làm suffix:

```text
Phone        = 84901234567
ReversedPhone = 76543210948
```

Muốn tìm đuôi `4567`, hệ thống đảo thành `7654`, rồi query:

```text
ReversedPhone starts with 7654
```

## 7. Order module

Yêu cầu:

- Lưu thông tin snapshot khách hàng tại lúc tạo đơn.
- Lưu snapshot sản phẩm tại lúc tạo dòng đơn.
- Dòng đơn có số lượng, đơn giá, chiết khấu, thành tiền.
- Tổng đơn gồm tổng số lượng và tổng tiền.
- Chỉ sửa được đơn `Draft`.
- Giải quyết 2 người cùng sửa đơn.

Đang dùng:

- Aggregate: `Order`
- Entity trong aggregate: `OrderLine`
- Value object:
  - `CustomerSnapshot`
  - `ProductSnapshot`
  - `Money`
- State machine:

```text
Draft
  -> PendingInventory
      -> Confirmed
      -> InventoryRejected
Confirmed
  -> Cancelled
```

### Tính tiền

Mỗi dòng:

```text
LineTotal = UnitPrice * Quantity * (1 - DiscountPercent / 100)
```

VND được làm tròn `AwayFromZero` trong `Money.Vnd`.

Ví dụ:

```text
UnitPrice = 1001
Quantity = 3
Discount = 12.5%
LineTotal = 1001 * 3 * 0.875 = 2627.625 -> 2628
```

### Sửa dòng đơn

Trước đây nếu replace lines bằng cách clear/add toàn bộ thì EF dễ gặp lỗi khi vẫn cùng `(OrderId, ProductId)`. Hiện tại `Order.ReplaceLines` update in-place:

- Product đã có: update quantity/discount/snapshot, giữ nguyên `OrderLine.Id`.
- Product bị bỏ: remove line.
- Product mới: add line mới.

Cách này giúp concurrency test ổn định hơn.

### Optimistic concurrency bằng ETag

Mỗi `Order` có `Version`. Chỉ Order edit/confirm/cancel yêu cầu `If-Match`; Product/Customer update chưa bắt buộc `If-Match`.

Tạo draft order:

```http
POST /api/orders
Authorization: Bearer <token>
Content-Type: application/json

{
  "customerId": "00000000-0000-0000-0000-000000000001",
  "lines": [
    {
      "productId": "00000000-0000-0000-0000-000000000002",
      "quantity": 2,
      "discountPercent": 10
    }
  ]
}

HTTP/1.1 201 Created
ETag: "1"
```

Khi client đọc order:

```http
GET /api/orders/{id}
Authorization: Bearer <token>
```

Response:

```http
ETag: "3"
```

Khi client update:

```http
PUT /api/orders/{id}/lines
Authorization: Bearer <token>
If-Match: "3"
Content-Type: application/json

[
  {
    "productId": "00000000-0000-0000-0000-000000000002",
    "quantity": 3,
    "discountPercent": 5
  }
]
```

Server xử lý:

```text
OrdersController
    -> Request.RequireVersion()
    -> ReplaceOrderLines(id, expectedVersion, lines)
    -> OrderCommandHandler.LoadAndCheck
    -> nếu version khác expectedVersion thì 409 Conflict
```

Tình huống 2 người cùng sửa:

```text
User A đọc order version 3
User B đọc order version 3
User A update với If-Match "3" -> OK, version lên 4
User B update với If-Match "3" -> 409 Conflict
```

Search order:

```http
GET /api/orders?from=2026-07-01T00:00:00Z&to=2026-08-01T00:00:00Z&customer=0901&page=1&pageSize=20
Authorization: Bearer <token>
```

Confirm order không chuyển thẳng sang `Confirmed`. Nó chuyển:

```text
Draft -> PendingInventory
```

sau đó chờ Inventory trả `inventory.stock-reserved.v1` thì Sales mới chuyển:

```text
PendingInventory -> Confirmed
```

Cancel chỉ hợp lệ khi order đã `Confirmed`:

```http
POST /api/orders/{id}/cancel
Authorization: Bearer <token>
If-Match: "4"
```

## 8. Inventory service

Yêu cầu:

- Service riêng.
- Database riêng.
- Quản lý available/reserved.
- Mục tiêu không miss event từ Sales.

Vị trí:

```text
src/Services/Inventory
```

Các phần chính:

| Project | Vai trò |
|---|---|
| `Inventory.Domain` | `InventoryItem`, `Reservation`, invariant tồn kho |
| `Inventory.Application` | Interface/DTO `IInventoryService` |
| `Inventory.Infrastructure` | EF Core, Kafka consumer, Outbox publisher |
| `Inventory.Api` | HTTP API adjust/get stock |

API ví dụ:

```http
POST /api/inventory/{productId}/adjust
Authorization: Bearer <token>
Content-Type: application/json

{
  "sku": "SKU-001",
  "quantityDelta": 100
}
```

Endpoint này cần role `Admin` hoặc `Warehouse`.

Get tồn kho:

```http
GET /api/inventory/{productId}
Authorization: Bearer <token>

HTTP/1.1 200 OK
{
  "productId": "...",
  "sku": "SKU-001",
  "available": 100,
  "reserved": 0,
  "version": 1
}
```

Khi Sales confirm order, Inventory nhận event:

```text
sales.order-confirmation-requested.v1
```

Inventory xử lý:

- Nếu đủ hàng:
  - trừ `Available`
  - cộng `Reserved`
  - tạo `Reservation`
  - phát `inventory.stock-reserved.v1`
- Nếu thiếu hàng:
  - không đổi tồn
  - phát `inventory.stock-rejected.v1`

Lưu ý: Sales chỉ được cancel sau khi order đã `Confirmed`. Khi cancel, Sales phát `sales.order-cancellation-requested.v1`; Inventory release reservation và phát `inventory.stock-released.v1`.

## 9. AuditLog

Yêu cầu:

- AuditLog lưu MongoDB.
- Nhận event qua Kafka.
- Không lưu trùng event.

Vị trí:

```text
src/Services/AuditLog
```

Đang dùng:

- `AuditLog.Worker`: Generic Host, không HTTP.
- `AuditLog.Infrastructure/Mongo/AuditConsumer.cs`: Kafka handler lưu Mongo.
- Mongo unique index trên `EventId`.

Audit worker subscribe nhiều topic:

```text
sales.audit.v1
inventory.audit.v1
sales.order-confirmation-requested.v1
sales.order-cancellation-requested.v1
inventory.stock-reserved.v1
inventory.stock-rejected.v1
inventory.stock-released.v1
```

Mỗi document audit có:

- `EventId`
- `EventType`
- `AggregateId`
- `Version`
- `CorrelationId`
- `CausationId`
- `OccurredAt`
- `Actor`
- `Payload`
- Kafka `Topic`, `Partition`, `Offset`

## 10. Kafka đang dùng gì và dùng như thế nào?

Chi tiết sâu hơn về vị trí code, cách khởi tạo, cách producer/consumer hoạt động và hướng tái sử dụng nằm ở [kafka-usage-guide.md](kafka-usage-guide.md).

Project dùng Kafka thông qua KafkaFlow:

```text
KafkaFlow
KafkaFlow.Microsoft.DependencyInjection
KafkaFlow.Serializer.JsonCore
```

Kafka broker chạy bằng Docker:

```text
apache/kafka:4.1.1
```

Compose dùng Kafka KRaft mode, không cần ZooKeeper.

### 10.1 Topic đang dùng

| Topic | Producer | Consumer | Mục đích |
|---|---|---|---|
| `sales.audit.v1` | Sales | AuditLog | Audit các thay đổi Sales |
| `inventory.audit.v1` | Inventory | AuditLog | Audit thay đổi tồn kho |
| `sales.order-confirmation-requested.v1` | Sales | Inventory, AuditLog | Sales yêu cầu reserve tồn |
| `sales.order-cancellation-requested.v1` | Sales | Inventory, AuditLog | Sales yêu cầu release tồn |
| `inventory.stock-reserved.v1` | Inventory | Sales, AuditLog | Inventory báo reserve thành công |
| `inventory.stock-rejected.v1` | Inventory | Sales, AuditLog | Inventory báo thiếu hàng |
| `inventory.stock-released.v1` | Inventory | Sales, AuditLog | Inventory báo release xong |

Version `.v1` ở cuối topic để sau này có thể thêm `.v2` mà không phá consumer cũ.

### 10.2 Consumer group đang dùng

| Consumer group | Service | Subscribe topic |
|---|---|---|
| `inventory-orders-v1` | Inventory | Sales order confirmation/cancellation |
| `sales-inventory-results-v1` | Sales | Inventory stock result |
| `audit-mongodb-v1` | AuditLog | Tất cả audit/integration topics |

Ý nghĩa consumer group:

- Mỗi group nhận một bản copy riêng của message.
- Inventory và AuditLog có group khác nhau nên cùng nhận được event từ Sales.
- Nếu scale nhiều instance cùng group, Kafka chia partition giữa các instance.

### 10.3 Partition key đang dùng

Khi publish outbox:

```csharp
await producer.ProduceAsync(row.Topic, envelope.AggregateId.ToString(), envelope);
```

Key là:

```text
AggregateId
```

Với order event, `AggregateId` chính là `orderId`.

Tác dụng:

- Các event cùng order đi vào cùng partition.
- Kafka giữ thứ tự event trong cùng partition.
- Tránh case event của cùng order bị xử lý đảo thứ tự.

### 10.4 Event envelope

Contract nằm ở:

```text
src/Shared/BuildingBlocks.Contracts/IntegrationEvents/Common/EventEnvelope.cs
```

Envelope gồm:

```text
eventId       : ID duy nhất của event, dùng dedup
eventType     : loại event, ví dụ OrderConfirmationRequested
aggregateId   : ID aggregate, ví dụ orderId
version       : version aggregate tại thời điểm phát event
correlationId : nối các event/request cùng workflow
causationId   : event trước đó gây ra event hiện tại
occurredAt    : thời điểm UTC
actor         : user/system tạo event
data          : payload JSON
```

Ví dụ flow confirm order:

```text
User confirm order
    -> Sales tạo eventId A: OrderConfirmationRequested
    -> Inventory xử lý A
    -> Inventory tạo eventId B: StockReserved
       correlationId = correlationId của A
       causationId = A
    -> Sales xử lý B, update order Confirmed
    -> AuditLog lưu cả A và B
```

### 10.5 Vì sao dùng Outbox?

Nếu Sales vừa update database vừa publish Kafka trực tiếp, có thể xảy ra lỗi:

```text
DB commit thành công
Kafka publish fail
=> đơn đã PendingInventory nhưng Inventory không nhận event
=> miss event
```

Outbox giải quyết bằng cách:

```text
Trong cùng DB transaction:
    1. Update aggregate
    2. Insert outbox_messages

Background publisher:
    3. Poll outbox_messages
    4. Publish Kafka
    5. Mark ProcessedAt
```

Vì Outbox nằm trong DB transaction với aggregate, nếu DB commit thành công thì event chắc chắn còn trong DB để publish lại.

### 10.6 Vì sao dùng Inbox?

Kafka delivery là at-least-once, nghĩa là consumer có thể nhận trùng message.

Nếu không có Inbox:

```text
Inventory nhận cùng OrderConfirmationRequested 2 lần
=> reserve tồn 2 lần
=> sai tồn kho
```

Inbox giải quyết bằng:

```text
inbox_messages(EventId) unique
```

Consumer xử lý:

```text
1. Insert EventId vào Inbox trong transaction
2. Nếu unique violation => message đã xử lý, return success
3. Nếu insert được => xử lý nghiệp vụ
4. Commit transaction
```

### 10.7 Retry, backoff, DLQ, replay

Outbox row có các cột:

```text
Attempts
NextAttemptAt
DeadLetteredAt
LockedUntil
LockId
LastError
```

Publisher xử lý:

```text
1. Chọn message ProcessedAt null, DeadLetteredAt null, NextAttemptAt <= now
2. Claim bằng LockId và LockedUntil
3. Publish Kafka
4. Nếu thành công: set ProcessedAt
5. Nếu lỗi:
      Attempts++
      LastError = exception
      NextAttemptAt = now + exponential backoff
6. Nếu Attempts >= MaxAttempts:
      DeadLetteredAt = now
      không retry tự động nữa
```

Replay:

- Sales có Hangfire job:
  - `ReplayOutboxMessageAsync(Guid eventId)`
  - `ReplayDeadLettersAsync(int take)`
- Inventory hiện có retry/backoff/DLQ trong `InventoryOutboxPublisher`; chưa có Hangfire replay endpoint riêng như Sales.
- Replay reset:
  - `Attempts = 0`
  - `DeadLetteredAt = null`
  - `NextAttemptAt = now`
  - clear lock/error

## 11. Redis đang dùng gì?

Redis dùng cho 2 mục đích:

### 11.1 Cache-aside

Product detail được cache trong Redis:

```text
catalog:product:{productId}
```

Flow:

```text
GET product
    -> check Redis
    -> nếu có: return cache
    -> nếu không: query DB, set cache
```

Khi update product:

```text
Update DB
Remove cache
```

### 11.2 Distributed lock

Redis lock dùng trong job cleanup:

```text
lock:jobs:sales-cleanup
```

Mục đích:

- Nếu nhiều instance Sales chạy cùng lúc, chỉ một instance cleanup Inbox/Outbox cũ.
- Không dùng Redis lock để đảm bảo correctness của Order.
- Correctness của Order dựa trên DB transaction và optimistic concurrency.

## 12. Hangfire đang dùng gì?

Hangfire dùng trong Sales API:

- Dashboard: `/hangfire`
- Storage: PostgreSQL
- Queues:
  - `critical`
  - `default`
  - `maintenance`

Job hiện có:

- `CleanupAsync`: xóa inbox/outbox cũ.
- `ReplayOutboxMessageAsync`: replay 1 event.
- `ReplayDeadLettersAsync`: replay batch DLQ.

## 13. Database và index

### Sales PostgreSQL

Lưu:

- Products
- Customers
- Orders
- OrderLines
- Identity users/roles/tokens
- Outbox/Inbox

Index chính:

- Product name: GIN trigram.
- Product SKU: unique.
- Customer name: GIN trigram.
- Customer phone: B-tree.
- Customer reversed phone: B-tree.
- Order created date.
- Order customer name: GIN trigram.
- Order customer phone.
- Outbox retry/DLQ index.

### Inventory PostgreSQL

Lưu:

- Inventory items
- Reservations
- Reservation lines
- Inventory Outbox/Inbox

Ràng buộc:

- `InventoryItem.ProductId` là key.
- `Reservation.OrderId` unique: mỗi order chỉ có một reservation hiệu lực.
- `Inbox.EventId` unique: chống duplicate event.

### MongoDB

AuditLog lưu collection:

```text
events
```

Unique index:

```text
EventId
```

## 14. Observability

Project có:

- Structured logging bằng Serilog.
- Log gửi Seq.
- Trace/metric bằng OpenTelemetry.
- OTel Collector forward sang Elastic APM.
- Kibana dùng xem trace/metric.

Custom metrics:

| Metric | Ý nghĩa |
|---|---|
| `sales.outbox.backlog` | Số Sales outbox chưa publish |
| `sales.outbox.deadletters` | Số Sales outbox vào DLQ |
| `sales.outbox.published` | Số event Sales publish thành công |
| `sales.outbox.failed` | Số lần publish Sales lỗi |
| `sales.inbox.duplicate` | Số event duplicate Sales bỏ qua |
| `inventory.outbox.backlog` | Số Inventory outbox chưa publish |
| `inventory.outbox.deadletters` | Số Inventory outbox vào DLQ |
| `inventory.reservation.rejected` | Số reservation thiếu hàng |
| `inventory.reservation.reserved` | Số reservation thành công |

Dashboard gợi ý nằm ở:

```text
docs/observability.md
```

## 15. Docker Compose

Chạy:

```bash
sudo docker compose -f docker/docker-compose.yml up -d --build
```

Services:

| Service | Port | Vai trò |
|---|---:|---|
| Sales API | 5000 | REST API Sales |
| Inventory API | 5001 | REST API Inventory |
| PostgreSQL | 5432 | Sales/Inventory/Hangfire DB |
| Redis | 6379 | Cache/lock |
| MongoDB | 27017 | AuditLog |
| Kafka | 9094 | Kafka external listener |
| Seq | 8081 | Structured logs |
| Elasticsearch | 9200 | Elastic storage |
| Kibana | 5601 | Dashboard |
| APM Server | 8200 | Elastic APM endpoint |
| OTel Collector | 4317/4318 | OTel receiver |

## 16. FE và test tools

### Angular test client

Vị trí:

```text
src/Web/Sales.TestClient
```

Chạy:

```bash
cd src/Web/Sales.TestClient
npm install
npm start
```

Mở:

```text
http://localhost:4200
```

FE hỗ trợ:

- Login.
- Create/search/update product.
- Create/search/update customer.
- Search order, load detail, create draft, update lines, confirm/cancel order.
- Test same ETag concurrency.
- Adjust/get inventory.

### Playwright smoke test

Vị trí:

```text
tests/Playwright
```

Chạy:

```bash
cd tests/Playwright
npm install
npm run test:smoke
```

Test gồm:

- health endpoints.
- login seeded admin.
- create/search product.
- create/search customer phone prefix/suffix.
- concurrency ETag: 2 update cùng ETag phải ra 1 success và 1 conflict.

### .NET tests

Chạy:

```bash
dotnet test Sales.sln
```

Test projects:

| Project | Mục đích |
|---|---|
| `Sales.Domain.Tests` | Invariant aggregate, money, state transition |
| `Sales.Application.Tests` | Mapping/application behavior |
| `Inventory.Tests` | Stock/reservation invariant |
| `AuditLog.Tests` | Audit document/Mongo dedup opt-in |
| `Sales.Architecture.Tests` | Dependency rule |
| `Sales.Infrastructure.Tests` | Outbox reliability opt-in |
| `Inventory.Infrastructure.Tests` | Inventory persistence reliability opt-in |

Reliability tests với Postgres/Mongo thật:

```bash
RUN_RELIABILITY_TESTS=true dotnet test Sales.sln
```

Override connection string:

```bash
SALES_TEST_POSTGRES="Host=localhost;Port=5432;Database=sales_reliability_tests;Username=postgres;Password=postgres"
INVENTORY_TEST_POSTGRES="Host=localhost;Port=5432;Database=inventory_reliability_tests;Username=postgres;Password=postgres"
MONGO_TEST_CONNECTION="mongodb://localhost:27017"
MONGO_TEST_DATABASE="audit_reliability_tests"
```

## 17. Demo flow để trình bày

### Flow 1: Product create/search/update

```text
1. Login admin
2. Create product SKU-001
3. Search name = keyboard
4. Chọn product trong Angular FE
5. Update name/price/isActive
6. Get product detail có ETag theo Version
7. Redis cache được set khi create/get detail và remove khi update
```

### Flow 2: Customer phone prefix/suffix

```text
1. Create customer phone +84 901 234 567
2. DB lưu Phone = 84901234567
3. DB lưu ReversedPhone = 76543210948
4. Search prefix 8490
5. Search suffix 4567
```

### Flow 3: Order concurrency

```text
1. Create draft order -> ETag "1"
2. Reload/detail order để lấy ETag hiện tại
3. Client A và B cùng dùng ETag đó
4. A update lines -> 200 OK, ETag tăng lên version mới
5. B update lines với ETag cũ -> 409 Conflict
```

### Flow 4: Confirm order và reserve inventory

```text
1. Sales confirm order
2. Sales update order -> PendingInventory
3. Sales insert outbox event OrderConfirmationRequested
4. SalesOutboxPublisher publish Kafka
5. Inventory consumer nhận event
6. Inventory insert Inbox chống trùng
7. Inventory reserve stock
8. Inventory insert outbox StockReserved
9. InventoryOutboxPublisher publish Kafka
10. Sales consumer nhận StockReserved
11. Sales update order -> Confirmed
12. AuditLog lưu toàn bộ event vào MongoDB
```

Nếu Inventory thiếu hàng:

```text
Inventory phát inventory.stock-rejected.v1
Sales nhận event
Sales update order -> InventoryRejected
Order lưu RejectionReason
```

Nếu muốn cancel/release:

```text
1. Chỉ cancel khi order đã Confirmed
2. Sales update order -> Cancelled
3. Sales phát sales.order-cancellation-requested.v1
4. Inventory release reserved stock
5. Inventory phát inventory.stock-released.v1
```

### Flow 5: Kafka down rồi phục hồi

```text
1. Sales confirm order, DB commit thành công
2. Kafka đang down nên SalesOutboxPublisher publish fail
3. Outbox row tăng Attempts, set NextAttemptAt
4. Kafka phục hồi
5. Publisher retry message
6. Publish thành công, set ProcessedAt
```

### Flow 6: Consumer nhận duplicate

```text
1. Inventory nhận eventId A lần 1
2. Insert Inbox A thành công, xử lý reserve
3. Inventory nhận eventId A lần 2
4. Insert Inbox A bị unique violation
5. Consumer coi là duplicate và return success
6. Không reserve lần 2
```

## 18. Những điểm có thể nói khi bảo vệ bài

### Vì sao chọn optimistic concurrency thay vì Redis lock?

Vì bài toán 2 người sửa cùng order là consistency của aggregate trong database. Redis lock có thể mất lock, timeout, hoặc không bảo vệ được mọi đường ghi. Do đó correctness dựa trên:

- `Order.Version`
- EF Core concurrency token
- HTTP `ETag` / `If-Match`
- DB transaction

Redis lock chỉ dùng cho scheduled job/cache rebuild.

### Vì sao dùng Outbox/Inbox mà không tin Kafka exactly-once?

Vì hệ thống gồm database và Kafka. Kafka exactly-once không tự làm transaction chung với Postgres của Sales/Inventory. Outbox đảm bảo DB commit thì event vẫn còn để publish. Inbox đảm bảo consumer xử lý duplicate an toàn.

### Vì sao AuditLog dùng MongoDB?

Audit event là dữ liệu append/read nhiều, schema payload có thể khác nhau theo event type. MongoDB phù hợp để lưu document audit dạng flexible JSON, đồng thời unique index `EventId` giúp dedup.

### Vì sao Order lưu snapshot?

Nếu Product/Customer đổi tên/giá/phone sau khi tạo order, order cũ vẫn phải giữ thông tin tại thời điểm bán. Vì vậy `OrderLine` lưu:

- `Sku`
- `ProductName`
- `UnitPrice`

Order lưu:

- `CustomerName`
- `CustomerPhone`

## 19. Lệnh chạy nhanh

Build/test:

```bash
dotnet restore Sales.sln --disable-parallel
dotnet build Sales.sln --no-restore
dotnet test Sales.sln --no-build --no-restore
```

Docker:

```bash
sudo docker compose -f docker/docker-compose.yml up -d --build
sudo docker compose -f docker/docker-compose.yml ps
```

Playwright:

```bash
cd tests/Playwright
npm run test:smoke
```

Angular test client:

```bash
cd src/Web/Sales.TestClient
npm start
```

Open:

```text
Sales API:      http://localhost:5000
Inventory API:  http://localhost:5001
Angular FE:     http://localhost:4200
Seq:            http://localhost:8081
Kibana:         http://localhost:5601
Hangfire:       http://localhost:5000/hangfire
```
