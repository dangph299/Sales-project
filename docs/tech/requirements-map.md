# Bản đồ yêu cầu

Tài liệu này đối chiếu từng yêu cầu bài thực hành với code hiện có. Mỗi mục có trạng thái, vị trí code chính, cách hoạt động và quy tắc cần nhớ khi mở rộng.

## 1. Danh mục sản phẩm và search tên sản phẩm

Trạng thái: đã đáp ứng.

Code chính:

- Domain aggregate: `src/Services/Sales/Sales.Domain/Aggregates/Product.cs`
- Command tạo/sửa/xóa: `src/Services/Sales/Sales.Application/Commands/Products/`
- Query search/get: `src/Services/Sales/Sales.Application/Queries/Products/`
- Read service search: `src/Services/Sales/Sales.Infrastructure/Persistence/ProductReadService.cs`
- Cache decorator: `src/Services/Sales/Sales.Infrastructure/Persistence/CachedProductReadService.cs`
- API controller: `src/Services/Sales/Sales.Api/Controllers/ProductsController.cs`
- EF mapping: `src/Services/Sales/Sales.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`

Cách hoạt động:

```mermaid
flowchart LR
    Client[Client] --> API[ProductsController]
    API --> MediatR[MediatR]
    MediatR --> Handler[SearchProductsHandler]
    Handler --> ReadService[IProductReadService]
    ReadService --> DB[(Sales PostgreSQL)]
    DB --> ReadService
    ReadService --> Handler
    Handler --> API
    API --> Client
```

- API gọi MediatR command/query.
- Command handler load aggregate qua repository, gọi hành vi domain, rồi commit bằng Unit of Work.
- Query handler gọi `IProductReadService`.
- Search tên sản phẩm dùng `EF.Functions.ILike`.
- Get product có cache-aside bằng Redis qua `CachedProductReadService`.

Cache-aside khi get product:

```mermaid
flowchart TD
    A[Get product by id] --> B{Redis cache có dữ liệu?}
    B -- Có --> C[Trả ProductDto từ cache]
    B -- Không --> D[Đọc ProductReadService từ PostgreSQL]
    D --> E{Có product?}
    E -- Có --> F[Ghi ProductDto vào Redis]
    F --> G[Trả ProductDto]
    E -- Không --> H[Trả null/404]
```

Quy tắc:

- Business rule của sản phẩm đặt trong `Product`.
- Command mới đặt trong `Sales.Application/Commands/Products/`.
- Query mới đặt trong `Sales.Application/Queries/Products/`.
- Không query EF Core trực tiếp trong controller.
- Nếu sửa dữ liệu product, cần remove hoặc update cache liên quan.

## 2. Khách hàng và search phone đầu/đuôi, tên khách hàng

Trạng thái: đã đáp ứng.

Code chính:

- Domain aggregate: `src/Services/Sales/Sales.Domain/Aggregates/Customer.cs`
- Command tạo/sửa/xóa: `src/Services/Sales/Sales.Application/Commands/Customers/`
- Query search/get: `src/Services/Sales/Sales.Application/Queries/Customers/`
- Enum phone match: `src/Services/Sales/Sales.Application/Common/Enums/PhoneMatch.cs`
- Read service: `src/Services/Sales/Sales.Infrastructure/Persistence/CustomerReadService.cs`
- API controller: `src/Services/Sales/Sales.Api/Controllers/CustomersController.cs`
- EF mapping: `src/Services/Sales/Sales.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs`

Cách hoạt động:

```mermaid
flowchart TD
    A[GET /api/customers] --> B[CustomersController]
    B --> C[SearchCustomers query]
    C --> D[CustomerReadService]
    D --> E{Name filter?}
    E -- Có --> F[ILike Name]
    E -- Không --> G[Skip name filter]
    F --> H{Phone filter?}
    G --> H
    H -- Prefix --> I[Phone.StartsWith]
    H -- Suffix --> J[ReversedPhone.StartsWith]
    H -- Không --> K[Paging]
    I --> K
    J --> K
    K --> L[(Sales PostgreSQL)]
```

- Search tên dùng `ILike`.
- Search phone đầu số dùng `Phone.StartsWith(normalized)`.
- Search phone đuôi số dùng cột `ReversedPhone` và `StartsWith` trên chuỗi phone đảo ngược.
- Phone được normalize về chữ số.

Quy tắc:

- Filter search mới nên thêm vào query record trước, rồi update read service.
- Filter cần EF-specific function thì đặt logic trong Infrastructure, không đặt trong Domain.
- Controller không được gọi `DbContext` trực tiếp cho use case nghiệp vụ.

## 3. Đơn hàng, tổng số lượng, tổng tiền, chi tiết đơn

Trạng thái: đã đáp ứng.

Code chính:

- Aggregate root: `src/Services/Sales/Sales.Domain/Aggregates/Order.cs`
- Entity line: `src/Services/Sales/Sales.Domain/Entities/OrderLine.cs`
- Value objects: `src/Services/Sales/Sales.Domain/ValueObjects/`
- DTO order: `src/Services/Sales/Sales.Application/DTOs/Orders/`
- Mapping DTO: `src/Services/Sales/Sales.Application/DTOs/DtoMapping.cs`
- API controller: `src/Services/Sales/Sales.Api/Controllers/OrdersController.cs`
- EF mapping order/line: `src/Services/Sales/Sales.Infrastructure/Persistence/Configurations/`

Cách hoạt động:

```mermaid
classDiagram
    class Order {
        Guid CustomerId
        string CustomerName
        string CustomerPhone
        OrderStatus Status
        long Version
        int TotalQuantity
        Money Total
        ReplaceLines()
        RequestConfirmation()
    }
    class OrderLine {
        Guid ProductId
        string Sku
        string ProductName
        int Quantity
        Money UnitPrice
        decimal DiscountPercent
        Money LineTotal
    }
    Order "1" *-- "many" OrderLine
```

- `Order` giữ snapshot khách hàng: `CustomerId`, `CustomerName`, `CustomerPhone`.
- `Order.TotalQuantity` tính tổng số lượng từ lines.
- `Order.Total` tính tổng tiền từ `OrderLine.LineTotal`.
- `OrderLine` có product, quantity, unit price, discount percent.
- DTO trả về order gồm thông tin khách hàng, status, total quantity, total, version và lines.

Quy tắc:

- Rule nghiệp vụ của đơn hàng đặt trong `Order`.
- Chỉ `Order` được điều khiển danh sách `OrderLine`.
- Nếu thêm trạng thái đơn hàng, cần update `OrderStatus`, domain methods, validators, mapping và tests.

## 4. Search đơn hàng theo ngày tạo, tên/số điện thoại khách hàng

Trạng thái: đã đáp ứng.

Code chính:

- Query record: `src/Services/Sales/Sales.Application/Queries/Orders/SearchOrders.cs`
- Query handler: `src/Services/Sales/Sales.Application/Queries/Orders/SearchOrdersHandler.cs`
- Read service: `src/Services/Sales/Sales.Infrastructure/Persistence/OrderReadService.cs`
- Specification base: `src/Services/Sales/Sales.Domain/Services/Specifications/`
- Specification EF-specific: `src/Services/Sales/Sales.Infrastructure/Persistence/Specifications/`
- API endpoint: `GET /api/orders` trong `OrdersController`

Cách hoạt động:

```mermaid
flowchart LR
    Client[Client] --> API[GET /api/orders]
    API --> Query[SearchOrders]
    Query --> Handler[SearchOrdersHandler]
    Handler --> ReadService[OrderReadService]
    ReadService --> Specs[Compose Specifications]
    Specs --> Filters[CreatedAt + CustomerName/Phone]
    Filters --> DB[(Sales PostgreSQL)]
    DB --> Result[PagedResult OrderDto]
```

- Filter `from` và `to` theo `CreatedAt`.
- Filter `customer` match tên hoặc phone khách hàng.
- Infrastructure compose specification rồi apply vào EF query.

Quy tắc:

- Specification trong Domain chỉ chứa expression thuần C#.
- Specification cần `EF.Functions` đặt trong Infrastructure.
- Luôn paging query để tránh trả về quá nhiều dữ liệu.

## 5. Hai người cùng sửa đơn hàng

Trạng thái: đã đáp ứng bằng optimistic concurrency.

Code chính:

- Aggregate version: `src/Shared/BuildingBlocks.Domain/Abstractions/AggregateRoot.cs`
- EF concurrency token: `src/Services/Sales/Sales.Infrastructure/Persistence/Configurations/OrderConfiguration.cs`
- ETag helper: `src/Services/Sales/Sales.Api/Extensions/ControllerEtagExtensions.cs`
- Controller dùng `If-Match`: `src/Services/Sales/Sales.Api/Controllers/OrdersController.cs`
- Conflict exception: `src/Services/Sales/Sales.Application/Services/ConflictException.cs`
- Exception mapping 409: `src/Services/Sales/Sales.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Test: `tests/Sales.Infrastructure.Tests/ConfirmOrderConcurrencyTests.cs`

Cách hoạt động:

```mermaid
sequenceDiagram
    participant A as User A
    participant B as User B
    participant API as Orders API
    participant DB as PostgreSQL

    A->>API: GET /api/orders/{id}
    API-->>A: Order + ETag "1"
    B->>API: GET /api/orders/{id}
    API-->>B: Order + ETag "1"
    A->>API: PUT /lines If-Match "1"
    API->>DB: Save with Version = 1
    DB-->>API: Success, Version becomes 2
    API-->>A: 200 + ETag "2"
    B->>API: PUT /lines If-Match "1"
    API->>DB: Save with stale Version = 1
    DB-->>API: Concurrency conflict
    API-->>B: 409 Conflict
```

- Mỗi aggregate có `Version`.
- Khi sửa order, client phải gửi header `If-Match` chứa version đang có.
- Handler load order và check `ExpectedVersion`.
- EF Core cũng dùng `Version` làm concurrency token.
- Nếu version cũ, API trả 409 Conflict.

Quy tắc:

- Endpoint sửa order phải đọc `Request.RequireVersion()`.
- Command phải có `ExpectedVersion`.
- Handler phải dùng `orders.LoadAndCheck(...)`.
- Response sau khi sửa phải set ETag mới bằng `Response.SetEtag(order)`.

## 6. AuditLog dùng MongoDB và Kafka

Trạng thái: đã đáp ứng.

Code chính:

- Worker entry: `src/Services/AuditLog/AuditLog.Worker/Program.cs`
- Worker DI: `src/Services/AuditLog/AuditLog.Worker/DependencyInjection.cs`
- Kafka handler: `src/Services/AuditLog/AuditLog.Infrastructure/Mongo/AuditEventHandler.cs`
- Mongo writer: `src/Services/AuditLog/AuditLog.Infrastructure/Mongo/MongoAuditWriter.cs`
- Mongo document: `src/Services/AuditLog/AuditLog.Infrastructure/Mongo/AuditDocument.cs`
- Mongo startup/index: `src/Services/AuditLog/AuditLog.Worker/Hosting/MongoStartupService.cs`
- Docker MongoDB: `docker/docker-compose.yml`

Cách hoạt động:

```mermaid
flowchart LR
    Kafka[Kafka topics] --> Worker[AuditLog.Worker]
    Worker --> Handler[AuditEventHandler]
    Handler --> Writer[MongoAuditWriter]
    Writer --> Mongo[(MongoDB events)]
    Writer --> Index[Unique EventId index]
```

- AuditLog.Worker consume nhiều topic Kafka.
- Mỗi message là `EventEnvelope`.
- `MongoAuditWriter` upsert theo `EventId`.
- Mongo có unique index trên `EventId` để tránh duplicate.

Quy tắc:

- Payload audit nên dùng contract `AuditChanged`.
- Topic phải khai báo trong `KafkaTopics`.
- Worker muốn consume topic mới thì thêm vào `AddAuditLogWorker`.
- AuditLog không được reference trực tiếp Sales.Domain hoặc Inventory.Domain.

## 7. Inventory service riêng, bảng riêng, mục tiêu không miss event

Trạng thái: đã refactor qua CQRS/MediatR và đã xử lý rủi ro event lệch thứ tự được phát hiện trong review.

Code chính:

- Inventory API: `src/Services/Inventory/Inventory.Api/`
- Inventory DbContext: `src/Services/Inventory/Inventory.Infrastructure/Persistence/DbContexts/InventoryDbContext.cs`
- Inventory item: `src/Services/Inventory/Inventory.Domain/Entities/InventoryItem.cs`
- Reservation aggregate: `src/Services/Inventory/Inventory.Domain/Aggregates/Reservation.cs`
- Kafka consumer: `src/Services/Inventory/Inventory.Infrastructure/Kafka/InventoryEventHandler.cs`
- Event adapter: `src/Services/Inventory/Inventory.Infrastructure/Kafka/InventoryIntegrationEventProcessor.cs`
- Reserve command: `src/Services/Inventory/Inventory.Application/Commands/ReserveStock/`
- Release command: `src/Services/Inventory/Inventory.Application/Commands/ReleaseStock/`
- Inbox row: `src/Services/Inventory/Inventory.Infrastructure/Persistence/Inbox/InboxRow.cs`
- Outbox publisher: `src/Services/Inventory/Inventory.Infrastructure/Kafka/InventoryOutboxPublisher.cs`

Cách hoạt động:

```mermaid
sequenceDiagram
    participant Sales as Sales.Api
    participant SOutbox as Sales Outbox
    participant Kafka as Kafka
    participant Inv as Inventory.Api
    participant IInbox as Inventory Inbox
    participant IOutbox as Inventory Outbox

    Sales->>SOutbox: Ghi OrderConfirmationRequested
    SOutbox->>Kafka: Publish event
    Kafka->>Inv: Consume event
    Inv->>IInbox: Insert EventId
    Inv->>Inv: Reserve hoặc reject stock
    Inv->>IOutbox: Ghi StockReserved/StockRejected
    IOutbox->>Kafka: Publish reply
    Kafka->>Sales: Consume reply
    Sales->>Sales: Update order status
```

- Sales confirm order rồi publish event qua Sales Outbox.
- Inventory Kafka adapter map envelope thành `ReserveStockCommand` hoặc `ReleaseStockCommand`.
- Application handler insert Inbox trước để idempotent.
- Application handler reserve/release stock trong transaction qua domain methods.
- Inventory enqueue kết quả vào Inventory Outbox.
- Inventory Outbox publish `StockReserved`, `StockRejected`, `StockReleased`.

Cần lưu ý:

- Hệ thống có Outbox/Inbox và retry, tốt cho mục tiêu không miss event.
- Case confirmation event mới đến trước release event cũ đã có xử lý delta và test trong `tests/Inventory.Tests/ReserveStockHandlerTests.cs`.

## 8. CQRS và MediatR

Trạng thái: Sales và Inventory đều đã đáp ứng bằng CQRS/MediatR cho các use case hiện hữu.

Code chính:

- CQRS marker: `src/Shared/BuildingBlocks.Application/Abstractions/Messaging/`
- MediatR behaviors: `src/Shared/BuildingBlocks.Application/Behaviors/`
- Sales commands: `src/Services/Sales/Sales.Application/Commands/`
- Sales queries: `src/Services/Sales/Sales.Application/Queries/`
- MediatR registration: `src/Services/Sales/Sales.Api/Extensions/ServiceCollectionExtensions.cs`
- Inventory commands: `src/Services/Inventory/Inventory.Application/Commands/`
- Inventory queries: `src/Services/Inventory/Inventory.Application/Queries/`
- Inventory transaction behavior: `src/Services/Inventory/Inventory.Application/Services/Behaviors/InventoryTransactionBehavior.cs`
- Inventory MediatR registration: `src/Services/Inventory/Inventory.Api/Extensions/ServiceCollectionExtensions.cs`

Inventory hiện tại:

- `Inventory.Api` nhận HTTP request và dispatch qua `ISender.Send(...)`.
- `Inventory.Infrastructure/Kafka/InventoryIntegrationEventProcessor.cs` là adapter kỹ thuật: map integration event sang `ReserveStockCommand` hoặc `ReleaseStockCommand`, rồi dispatch qua MediatR.
- Command/query handlers nằm trong `Inventory.Application`, điều phối use case qua repository/read-service ports và `IUnitOfWork`.
- `InventoryTransactionBehavior` mở transaction cho idempotent commands; shared validation/logging behaviors đến từ `BuildingBlocks.Application`.
- `Inventory.Infrastructure` giữ EF Core persistence, Kafka, Inbox/Outbox, metrics và adapter kỹ thuật; business workflow không nằm trong Kafka handler.

Mô hình:

```mermaid
flowchart TD
    Controller[Controller] --> Sender[MediatR ISender]
    Sender --> Command[Command]
    Sender --> Query[Query]
    Command --> CommandHandler[Command Handler]
    Query --> QueryHandler[Query Handler]
    CommandHandler --> Aggregate[Domain Aggregate]
    CommandHandler --> Uow[Unit of Work]
    QueryHandler --> ReadService[Read Service]
    ReadService --> DB[(Database)]
    Uow --> DB
```

Quy tắc:

- Command làm thay đổi dữ liệu.
- Query chỉ đọc dữ liệu.
- Handler không chứa logic HTTP.
- Controller chỉ parse request và gọi `ISender.Send`.

## 9. Factory Method

Trạng thái: đã đáp ứng theo dạng static factory method cho object có rule.

Code chính:

- Domain factory: `Product.Create`, `Customer.Create`, `Order.Create`
- Line factory: `OrderLine.Create`
- Reservation factory: `Reservation.Create`
- Event envelope factory: `src/Shared/BuildingBlocks.Infrastructure/Events/EventEnvelopeFactory.cs`
- Domain event mapper: `src/Services/Sales/Sales.Infrastructure/Kafka/DomainEventMapper.cs`

Quy tắc:

- Nếu tạo aggregate/entity cần enforce invariant, dùng static factory method thay vì public constructor.
- Factory domain không gọi database, Kafka, Redis.
- Factory tạo event envelope nằm ở Infrastructure vì liên quan serialize/transport.

## 10. Redis cache và distributed lock

Trạng thái: đã đáp ứng.

Code chính:

- Redis registration: `src/Services/Sales/Sales.Infrastructure/DependencyInjection.cs`
- Cache abstraction: `src/Services/Sales/Sales.Application/Interfaces/ICacheService.cs`
- Cache base: `src/Services/Sales/Sales.Infrastructure/ExternalServices/CacheService.cs`
- Product cache: `src/Services/Sales/Sales.Infrastructure/ExternalServices/ProductCache.cs`
- Cache usage: `src/Services/Sales/Sales.Infrastructure/Persistence/CachedProductReadService.cs`
- Distributed lock: `src/Services/Sales/Sales.Infrastructure/Hangfire/MaintenanceJobs.cs`
- Docker Redis: `docker/docker-compose.yml`

Quy tắc:

- Cache dùng cho read model, không dùng làm nguồn sự thật.
- Cache miss thì đọc DB, sau đó set cache.
- Khi data thay đổi, cần remove/update cache liên quan.
- Distributed lock chỉ để tránh nhiều instance chạy job trùng nhau, không thay thế transaction DB.

## 11. Repository và Unit of Work

Trạng thái: đã đáp ứng trong Sales.

Code chính:

- Repository interface chung: `src/Services/Sales/Sales.Domain/Repositories/IRepository.cs`
- Repository đặc thù: `IProductRepository`, `IOrderRepository`
- Repository implementation: `src/Services/Sales/Sales.Infrastructure/Repositories/`
- Unit of Work interface: `src/Shared/BuildingBlocks.Application/Persistence/IUnitOfWork.cs`
- Unit of Work implementation: `src/Services/Sales/Sales.Infrastructure/UnitOfWork/UnitOfWork.cs`
- DbContext enqueue outbox khi save: `src/Services/Sales/Sales.Infrastructure/Persistence/DbContexts/SalesDbContext.cs`

Quy tắc:

- Repository chỉ dùng cho command-side aggregate.
- Read/search nên dùng read service riêng.
- Handler gọi domain behavior rồi `uow.SaveChangesAsync`.
- `SaveChangesAsync` là nơi commit state và enqueue domain event vào outbox.

## 12. Mapster

Trạng thái: đã đáp ứng trong Sales.Application.

Code chính:

- Package Mapster: `src/Services/Sales/Sales.Application/Sales.Application.csproj`
- Mapping extension: `src/Services/Sales/Sales.Application/DTOs/DtoMapping.cs`

Cách hoạt động:

- Product/Customer map bằng Mapster.
- Order map tay vì có nested lines, value object Money và calculated fields.

Quy tắc:

- Mapping đơn giản có thể dùng Mapster.
- Mapping phức tạp nên map tay để dễ đọc.
- Mapping không được query database.

## 13. Hangfire queue

Trạng thái: đã đáp ứng.

Code chính:

- Hangfire registration: `src/Services/Sales/Sales.Api/Extensions/ServiceCollectionExtensions.cs`
- Dashboard: `src/Services/Sales/Sales.Api/Extensions/ApplicationBuilderExtensions.cs`
- Job: `src/Services/Sales/Sales.Infrastructure/Hangfire/MaintenanceJobs.cs`
- Startup recurring job: `src/Services/Sales/Sales.Api/Extensions/StartupTaskExtensions.cs`
- Inventory cleanup worker: `src/Services/Inventory/Inventory.Infrastructure/Maintenance/InventoryMaintenanceWorker.cs`
- Inventory cleanup service: `src/Services/Inventory/Inventory.Infrastructure/Maintenance/InventoryMaintenanceService.cs`

Cách hoạt động:

```mermaid
flowchart TD
    A[Hangfire recurring job] --> B{Redis lock lấy được?}
    B -- Không --> C[Instance này bỏ qua]
    B -- Có --> D[Cleanup processed Inbox/Outbox]
    D --> E[Release lock bằng token]
```

- Hangfire dùng PostgreSQL storage.
- Server có queues: `critical`, `default`, `maintenance`.
- Sales cleanup chạy queue `maintenance`.
- Redis lock ngăn nhiều Sales instance cleanup cùng lúc.
- Inventory cleanup không dùng Hangfire; hosted worker dùng Postgres advisory transaction lock.

## 14. Kafka topic, group, partition

Trạng thái: đã đáp ứng topic/group; topic local được khởi tạo chủ động với 3 partitions và replication factor 1.

Code chính:

- Topic constants: `src/Shared/BuildingBlocks.Contracts/Messaging/KafkaTopics.cs`
- Consumer group constants: `src/Shared/BuildingBlocks.Contracts/Messaging/KafkaConsumerGroups.cs`
- Topic init script: `docker/kafka-init-topics.sh`
- Docker init service: `docker/docker-compose.yml` service `kafka-init`
- Sales Kafka registration: `src/Services/Sales/Sales.Infrastructure/DependencyInjection.cs`
- Inventory Kafka registration: `src/Services/Inventory/Inventory.Infrastructure/DependencyInjection.cs`
- Audit Kafka registration: `src/Services/AuditLog/AuditLog.Worker/DependencyInjection.cs`
- Publisher: `src/Shared/BuildingBlocks.Infrastructure/Kafka/KafkaOutboxPublisher.cs`

Cách partition/order hoạt động:

- `KafkaOutboxPublisher` dùng `EventEnvelope.AggregateId` làm Kafka message key.
- Các event của cùng aggregate được route vào cùng partition khi topic có nhiều partition.
- Kafka chỉ đảm bảo ordering trong phạm vi một partition, không đảm bảo global ordering trên toàn topic.
- Consumer group quyết định load balancing: nhiều instance cùng group chia nhau partitions; group khác nhau nhận bản sao độc lập của cùng topic.
- Local Docker tạo các topic trong `KafkaTopics` với 3 partitions và replication factor 1 qua `kafka-init`. Script dùng `--if-not-exists` nên chạy lại compose không lỗi nếu topic đã tồn tại.

Quy tắc:

- Không hardcode tên topic trong handler.
- Topic mới phải thêm vào `KafkaTopics`.
- Consumer group mới phải thêm vào `KafkaConsumerGroups`.
- Producer nên publish thông qua Outbox.
- Docker init đọc topic từ `KafkaTopics.cs`; không thêm danh sách topic trùng lặp trong compose/script.

## 15. Database: PostgreSQL và MongoDB

Trạng thái: đã đáp ứng.

PostgreSQL:

- Sales DB: `SalesDbContext`
- Inventory DB: `InventoryDbContext`
- Hangfire DB/storage: connection string Hangfire
- Docker Postgres: `docker/docker-compose.yml`

MongoDB:

- Audit document: `AuditDocument`
- Mongo writer: `MongoAuditWriter`
- Docker Mongo: `docker/docker-compose.yml`

Quy tắc:

- EF migrations nằm trong `Persistence/Migrations/`.
- Không sửa migration generated bằng tay nếu không cần.
- Mongo index tạo trong startup service, không tạo lại mỗi message.

## 16. Docker, docker-compose

Trạng thái: đã đáp ứng.

Code chính:

- Compose: `docker/docker-compose.yml`
- Sales Dockerfile: `src/Services/Sales/Sales.Api/Dockerfile`
- Inventory Dockerfile: `src/Services/Inventory/Inventory.Api/Dockerfile`
- Audit worker Dockerfile: `src/Services/AuditLog/AuditLog.Worker/Dockerfile`

Services trong compose:

```mermaid
flowchart LR
    Client[Client] --> Sales[Sales API]
    Client --> Inventory[Inventory API]
    Sales --> Postgres[(Postgres)]
    Inventory --> Postgres
    Sales --> Redis[(Redis)]
    Sales --> Kafka[(Kafka)]
    Inventory --> Kafka
    Kafka --> Audit[Audit Worker]
    Audit --> Mongo[(MongoDB)]
    Sales --> OTel[OTel Collector]
    Inventory --> OTel
    Audit --> OTel
    OTel --> Elastic[(Elasticsearch/APM)]
    Elastic --> Kibana[Kibana]
    Sales --> Seq[Seq]
    Inventory --> Seq
    Audit --> Seq
```

- postgres
- redis
- mongo
- kafka
- seq
- elasticsearch
- kibana
- apm-server
- otel-collector
- sales-api
- inventory-api
- audit-worker

## 17. Monitoring, logging, tracing, metrics

Trạng thái: đã đáp ứng phần chính; demo monitoring được mô tả trong [monitoring-demo.md](monitoring-demo.md). Chưa có dashboard export hoặc screenshot thật được commit.

Code chính:

- Shared Serilog: `src/Shared/BuildingBlocks.Observability/SerilogBootstrap.cs`
- OpenTelemetry registration: `src/Shared/BuildingBlocks.Web/Observability/OpenTelemetryExtensions.cs`
- Request observability middleware: `src/Shared/BuildingBlocks.Web/RequestObservabilityMiddleware.cs`
- Sales metrics: `src/Services/Sales/Sales.Infrastructure/Observability/`
- Inventory metrics: `src/Services/Inventory/Inventory.Infrastructure/Observability/`
- Docker Seq/Elastic/Kibana/APM/OTel: `docker/docker-compose.yml`

Mô hình:

```mermaid
flowchart LR
    App[Sales/Inventory/Audit services] --> Logs[Serilog logs]
    App --> Traces[OpenTelemetry traces]
    App --> Metrics[OpenTelemetry metrics]
    Logs --> Seq[Seq]
    Logs --> OTel[OTel Collector]
    Traces --> OTel
    Metrics --> OTel
    OTel --> APM[APM Server]
    APM --> Elastic[(Elasticsearch)]
    Elastic --> Kibana[Kibana dashboard]
```

Quy tắc:

- Log nên có correlation/trace id.
- Kafka consume/publish nên tạo activity span.
- Metrics nên đo backlog, failed, processed, duplicate cho outbox/inbox.
- Dashboard là phần vận hành; code chỉ export telemetry.
