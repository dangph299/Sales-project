# Architecture Checklist

Checklist hiện trạng kiến trúc theo từng project, cập nhật sau đợt hoàn thiện skeleton (xem `CODING_RULES.md` cho quy tắc chi tiết). Dùng để review PR: mọi thay đổi cấu trúc mới nên đối chiếu lại checklist này.

---

## Sales.Api

- **Required folders**: `Controllers/`, `Middleware/`, `Filters/`, `Extensions/` — ✅ đã có đủ.
- **Required base types**: `ExceptionHandlingMiddleware` (✅, `Middleware/` — duy nhất còn lại ở đây), DI registration trong `Program.cs` gọi `AddSalesApplication()` + `AddSalesInfrastructure()` (✅).
- **Optional types**: Request/Response model riêng cho từng endpoint khi body khác DTO nội bộ (đã có: `LoginRequest`, `RefreshRequest`, `UpdateProductRequest`, `UpdateCustomerRequest`, mỗi loại 1 file trong `Models/Requests/` — tách khỏi `Controllers/` vì đây là HTTP wire shape, không phải type của controller; body cố ý khác shape với Application command tương ứng, ví dụ `UpdateCustomerRequest` không có `Id` vì `Id` lấy từ route, controller ghép route + body thành command đầy đủ).
- **Forbidden dependencies**: không gọi trực tiếp `SalesDbContext`/EF Core từ Controller (phải qua MediatR `ISender`) — hiện tại tuân thủ, trừ `AuthController` (dùng `SalesDbContext` trực tiếp cho refresh-token, đây là ngoại lệ có chủ đích vì Identity/Auth không đi qua CQRS).
- **Notes**: middleware log HTTP request (`CorrelationLoggingMiddleware`/`HttpLoggingMiddleware` cũ) đã chuyển ra `Shared/BuildingBlocks.Web/RequestObservabilityMiddleware.cs` dùng chung với Inventory.Api — nếu tìm không thấy 2 file cũ này trong `Middleware/` thì đây là lý do, không phải bị xoá nhầm. Xem `Shared/BuildingBlocks.Web` ở dưới.

## Sales.Domain

- **Required folders**: `Aggregates/`, `Entities/`, `ValueObjects/`, `Events/`, `Exceptions/`, `Repositories/`, `Services/` — ✅ đã có đủ.
- **Required base types**: `AggregateRoot` (✅, giờ kế thừa `Entity`), `Entity` (✅ mới thêm — base cho entity con trong aggregate, ví dụ `OrderLine`), `IDomainEvent`/`DomainEvent` (✅), `DomainException` (✅), `IRepository<T>` (✅), `IProductRepository`/`IOrderRepository` (✅, giữ vì có query đặc thù). Không có `ICustomerRepository` vì Customer chỉ cần CRUD chung qua `IRepository<Customer>`.
- **Optional types**: Domain Service (khi rule nghiệp vụ không thuộc tự nhiên về 1 aggregate), Specification (khi cần compose query rule tái sử dụng).
  - `Services/Orders/` hiện để **`.gitkeep`** — chưa có rule nào trong `Order` cần domain service riêng (toàn bộ invariant đã nằm đúng trong aggregate `Order`).
  - `Services/Specifications/` (✅ mới) — `ISpecification<T>`/`Specification<T>` (abstract base, hỗ trợ `And()` compose qua `Expression.AndAlso` + `ExpressionVisitor` thay parameter). Domain chỉ chứa abstraction thuần (`System.Linq.Expressions`, không EF Core). Specification cụ thể cho `Order` (`OrderCreatedFromSpecification`, `OrderCreatedToSpecification`, `OrderCustomerMatchesSpecification`) nằm ở `Sales.Infrastructure/Persistence/Specifications/` vì cần `EF.Functions.ILike` — Domain không được phép biết EF Core. `OrderReadService.SearchAsync` compose 3 spec tuỳ theo filter nào có mặt rồi apply đúng 1 lần `.Where()`, thay cho 3 lần `.Where()` rời rạc trước đây (cùng hành vi, có test `SpecificationTests.cs` phủ logic `And()`).
- **Forbidden dependencies**: EF Core, MediatR, Kafka, Hangfire, MongoDB.Driver, Sales.Application, Sales.Infrastructure — enforce bằng `Sales.Architecture.Tests.DependencyRulesTests.Sales_domain_does_not_depend_on_outer_layers_or_other_services`.
- **Notes**: `Events/` chia theo aggregate (`Events/Products/`, `Events/Customers/`, `Events/Orders/`), file chung (`IDomainEvent.cs`, `DomainEvent.cs`) nằm ở gốc `Events/`.

## Sales.Application

- **Required folders**: `Commands/<Aggregate>/`, `Queries/<Aggregate>/`, `DTOs/<Aggregate>/`, `Interfaces/`, `Validators/<Aggregate>/`, `Services/Behaviors/`, `Common/Enums/` (✅ mới — enum dùng chung nhiều layer trong Sales, ví dụ `PhoneMatch`, không thuộc riêng 1 query nên không đặt trong `Queries/Customers/`) — ✅ đã có đủ.
- **Required base types**: Command/query record + handler (1 file/type, ✅ toàn bộ 8 command + 6 query), `ValidationBehavior` (✅), `LoggingBehavior` (✅, log Debug theo dõi tiến trình), `ErrorLoggingBehavior` (✅ mới thêm — nơi duy nhất log lỗi command/query, phân biệt Warning cho lỗi nghiệp vụ dự đoán được và Error cho lỗi bất ngờ), `CommonValidationRules` (✅ gom rule FluentValidation lặp), `Paging` (✅ chuẩn hóa page/pageSize cho read service), `AddSalesApplication()` DI extension (✅ đăng ký validator + 3 behavior theo thứ tự `ErrorLoggingBehavior` → `LoggingBehavior` → `ValidationBehavior`, ngoài nhất bọc trong cùng để bắt được exception từ 2 behavior kia).
- **Optional types**: `TransactionBehavior` — **chưa tạo**, vì mỗi handler hiện tại chỉ chạm 1 `SaveChangesAsync` trên `SalesDbContext` (đã là 1 unit-of-work atomic sẵn); thêm transaction behavior lúc này chỉ là lồng transaction thừa, không có lợi ích thực — để dành nếu sau này có handler cần ghi nhiều aggregate trong 1 command.
  - `Services/Specifications/` để `.gitkeep` — chưa có query nào cần compose filter phức tạp (các `SearchX` hiện filter trực tiếp trong Infrastructure read-service).
- **Forbidden dependencies**: Infrastructure, EF Core, KafkaFlow, `BuildingBlocks.Contracts` (Application hiện **không** cần contract liên service — enforce bằng `Sales_application_does_not_depend_on_infrastructure_or_other_services` test).
- **Notes**: Mapster **có được dùng thật** trong `DtoMapping.cs` — `ProductDto`/`CustomerDto` map qua `.Adapt<T>()` với `TypeAdapterConfig` tùy chỉnh; chỉ `OrderDto` map tay bằng constructor (vì `Order` có collection lồng và nhiều `Money.Amount` cần unwrap, map tay rõ ràng hơn). Ghi chú cũ ở dòng này ("Mapster không được dùng") là sai, đã sửa lại sau khi rà code trực tiếp — xem `docs/Sumaries-guide.md` mục 2.

## Sales.Infrastructure

- **Required folders**: `Persistence/{DbContexts,Configurations,Migrations,Outbox,Inbox}/`, `Repositories/`, `Kafka/`, `Hangfire/`, `ExternalServices/`, `UnitOfWork/` — ✅ đã có đủ.
- **Required base types**: `SalesDbContext` (✅, `OnModelCreating` giờ dùng `ApplyConfigurationsFromAssembly`), 7 `IEntityTypeConfiguration<T>` (✅), `Repository<T>` + 2 repository implementation cụ thể (`ProductRepository`, `OrderRepository`, ✅), `OutboxMessage`/`InboxMessage` (✅), `IOutboxPublisher` + `KafkaOutboxPublisher` (✅ mới — Strategy pattern cho outbox transport), `UnitOfWork` (✅ — wrapper mỏng implement `IUnitOfWork`, delegate thẳng `SalesDbContext.SaveChangesAsync`), `AddSalesInfrastructure()` DI extension (✅, tách private extension theo nhóm repositories/read services/context/cache/messaging).
- **Optional types**: Options class (`KafkaOptions`, `RedisOptions`...) — **chưa tạo**; hiện đọc trực tiếp qua `IConfiguration` (`GetConnectionString`, `configuration["Kafka:Brokers"]`). Để dành cho sub-project sau nếu muốn strongly-typed config toàn bộ.
- **Forbidden dependencies**: không có (Infrastructure được phép dùng mọi framework).
- **Notes**: `UnitOfWork/UnitOfWork.cs` tách khỏi `SalesDbContext` để `IUnitOfWork` không lộ toàn bộ surface của DbContext ra Application — không đổi hành vi (vẫn gọi đúng `SalesDbContext.SaveChangesAsync`, nơi có logic map domain event → outbox). Đã fix 1 bug thật trong lúc thêm `IOutboxPublisher`: đăng ký Scoped nhưng bị Singleton `SalesOutboxPublisher` (BackgroundService) inject trực tiếp → đổi sang `AddSingleton` (xem báo cáo).

## Refactor Review Notes

| Area | Duplicate Pattern | Current Problem | Proposed Abstraction | Action | Risk |
|---|---|---|---|---|---|
| Sales repositories | CRUD methods per aggregate | Copy/paste risk when adding aggregate repositories | `IRepository<T>` + `Repository<T>` | Kept generic CRUD; specific repos only for `GetBySkuAsync` and `GetWithLinesAsync` | Low |
| Order command support | Loading products one by one | Repeated repository calls and possible N+1 query pattern | `GetByIdsAsync` bulk load via generic repository | `Materialize` now loads distinct product ids once | Low |
| Validators | Id/version/phone/order-line uniqueness rules | Same FluentValidation expressions repeated | `CommonValidationRules` extension methods | Applied to Customer/Order/Product validators where duplicated | Low |
| Read services | `page = Math.Max...` / `Math.Clamp...` | Paging bounds repeated in each search | `Paging.Normalize(...)` | Applied to Product/Customer/Order read services | Low |
| DI registration | One growing Infrastructure method | Harder to add feature-specific registrations cleanly | Private `IServiceCollection` extension methods | Split repositories/read services/context/cache/messaging groups | Low |
| Cross-cutting handlers | Validation/logging | Already handled consistently | MediatR pipeline behaviors | No new behavior added | None |

## Inventory.Api

- **Required folders**: không dùng MVC Controllers (Minimal API), Program.cs là entrypoint duy nhất — ✅ phù hợp với quy mô service.
- **Required base types**: `AdjustStockRequest` (✅, tách file riêng), `public partial class Program;` (✅, marker cho test host).
- **Optional types**: không có.
- **Forbidden dependencies**: không gọi `InventoryDbContext` trực tiếp trong Program.cs ngoài `Database.MigrateAsync()` lúc khởi động — ✅ tuân thủ, toàn bộ business logic đi qua `IInventoryService`.
- **Notes**: Inventory hiện **không dùng MediatR/CQRS** (khác Sales) — `IInventoryService` được gọi trực tiếp từ Minimal API endpoint. Đây là kiến trúc có chủ đích cho 1 service nhỏ, không tự ý đổi sang CQRS handler vì đó là thay đổi kiến trúc thật, không phải "hoàn thiện skeleton".

## Inventory.Domain

- **Required folders**: `Aggregates/`, `Entities/`, `ValueObjects/`, `Events/`, `Exceptions/`, `Repositories/`, `Services/` — ✅ đã tạo đủ (một số vừa tạo mới ở đợt này).
- **Required base types**: `InventoryItem` (✅), `Reservation`/`ReservationLine` (✅, tách file), `ReservationRequestLine`/`ReservationStatus` (✅, tách file).
- **Optional types / Deferred**:
  - `Events/`, `Exceptions/`, `Repositories/`, `Services/` để **`.gitkeep`** — Inventory hiện KHÔNG có domain event nội bộ (integration event được enqueue trực tiếp từ `InventoryService`/`InventoryEventHandler`, không qua `AggregateRoot.Raise()` như Sales), KHÔNG có `DomainException` riêng (dùng `InvalidOperationException` thẳng — xem lý do dưới), KHÔNG có repository abstraction (`InventoryService` query trực tiếp qua `InventoryDbContext`).
  - **Không đổi 2 điểm trên** vì: (1) `tests/Inventory.Tests/InventoryItemTests.cs` assert cụ thể `Assert.Throws<InvalidOperationException>(...)` — đổi sang `DomainException` sẽ phá test và đổi kiểu exception (đổi behavior thật, không phải skeleton); (2) thêm Repository/UnitOfWork cho Inventory yêu cầu refactor `InventoryService`/`InventoryEventHandler` đang hoạt động tốt — vượt phạm vi "hoàn thiện skeleton".
- **Forbidden dependencies**: Inventory.Application, Inventory.Infrastructure, Sales.*, AuditLog.* — enforce bằng `Inventory_domain_is_isolated` test.

## Inventory.Application

- **Required folders**: `Interfaces/`, `DTOs/` — ✅ (`DTOs/` mới tạo ở đợt này để tách 3 snapshot record ra khỏi `IInventoryService.cs`).
- **Required base types**: `IInventoryService` (✅, tách riêng khỏi DTO), `InventorySnapshot`/`ReservationSnapshot`/`ReservationLineSnapshot` (✅, 1 file/type).
- **Optional types**: Commands/Queries/Validators/Behaviors — **không tạo**, vì Inventory không dùng CQRS (xem note ở Inventory.Api).
- **Forbidden dependencies**: Inventory.Infrastructure.
- **Notes**: nếu sau này Inventory cần thêm nghiệp vụ phức tạp (nhiều bước, nhiều rule), nên đánh giá lại việc chuyển sang CQRS + MediatR để đồng bộ với Sales — đây là quyết định kiến trúc cần thảo luận riêng, không nằm trong đợt hoàn thiện skeleton này.

## Inventory.Infrastructure

- **Required folders**: `Persistence/{DbContexts,Configurations,Migrations,Outbox,Inbox}/`, `Kafka/`, `Observability/`, `Services/` — ✅ đã có đủ.
- **Required base types**: `InventoryDbContext` (✅, `OnModelCreating` dùng `ApplyConfigurationsFromAssembly`), 5 `IEntityTypeConfiguration<T>` (✅), `InboxRow`/`OutboxRow` (✅, tách file), `IOutboxPublisher` + `KafkaOutboxPublisher` (✅ mới), `AddInventoryInfrastructure()` DI extension (✅).
- **Optional types**: Repository implementation — không có (không cần, vì Domain không có repository interface — xem Inventory.Domain).
- **Forbidden dependencies**: không có.
- **Notes**: cùng bug Scoped/Singleton như Sales đã được phát hiện và fix ở đây trước (do `dotnet ef migrations add` build full host và validate DI graph) — root cause chung, fix chung (`AddSingleton<IOutboxPublisher, KafkaOutboxPublisher>()`).

## AuditLog.Worker

- **Required base types**: `KafkaBusService`, `MongoStartupService` (Hosting/, ✅), `AddAuditLogWorker()` DI extension (✅ mới — trước đây mọi thứ nằm inline trong `Program.cs`).
- **Required setup**: Kafka consumer registration cho 7 topic qua `KafkaTopics.*` (✅, không còn magic string).
- **Optional types**: không có.
- **Forbidden dependencies**: không có domain logic — Worker chỉ consume `EventEnvelope` và ghi MongoDB qua `AuditLog.Infrastructure`, không chứa business rule của Sales/Inventory.
- **Notes**: `Program.cs` giờ chỉ còn 4 dòng thực thi + gọi `AddAuditLogWorker(...)`.

## AuditLog.Infrastructure

- **Required folders**: `Mongo/`, `Options/` (mới) — ✅.
- **Required base types**: `AuditDocument` (✅, tách khỏi handler), `AuditEventHandler` (✅), `IAuditWriter` + `MongoAuditWriter` (✅ mới — Repository/writer abstraction cho MongoDB, tách khỏi handler để handler không biết chi tiết MongoDB driver), `MongoOptions` (✅ mới — connection string + database name qua Options pattern), `AddAuditLogInfrastructure()` DI extension (✅ mới).
- **Optional types**: không có — AuditLog là read/write log service đơn giản theo đúng yêu cầu, không cần domain phức tạp.
- **Forbidden dependencies**: Sales.*, Inventory.* — AuditLog chỉ biết `EventEnvelope`/`BuildingBlocks.Contracts`, không reference domain của service khác.
- **Notes**: bug thật đã tồn tại trước đợt review này (index MongoDB bị tạo lại mỗi message, gây lỗi `IndexOptionsConflict` và chặn toàn bộ audit log ghi vào DB) đã được fix ở phiên trước — đợt này chỉ tái tổ chức file, không đổi lại logic đó. Index chỉ còn được tạo đúng 1 lần qua `MongoStartupService` → `IAuditWriter.EnsureIndexesAsync()`.

## Shared/BuildingBlocks.Contracts

- **Required folders**: `IntegrationEvents/{Common,Sales,Inventory}/`, `Messaging/` — ✅.
- **Required base types**: `IntegrationEventBase` (✅ — marker abstract record, áp dụng cho `AuditChanged`, `OrderConfirmationRequested`, `OrderCancellationRequested`, `StockReserved`, `StockRejected`, `StockReleased`), `EventEnvelope` (✅, Common — transport envelope thuần data, không kế thừa `IntegrationEventBase` vì nó là lớp bọc ngoài, không phải payload; **không** chứa factory method — xem note), `KafkaTopics` (✅ — 7 hằng số topic), `KafkaConsumerGroups` (✅ — hằng số group-id, `Messaging/`), `MessageHeaders` (✅ — hằng số tên header Kafka, gồm `TraceParent`/`TraceState` cho W3C trace propagation, `Messaging/`), `TraceContextParser` (✅ mới — parse `traceparent`/`tracestate` thành `ActivityContext`, dùng chung bởi cả 3 Kafka consumer handler thay vì mỗi handler tự viết riêng, `Messaging/`), đã wiring vào toàn bộ nơi từng dùng magic string.
- **Optional types**: không có thêm — mỗi contract hiện có đều tương ứng 1 flow thật (Sales→Inventory: `OrderConfirmationRequested`/`OrderCancellationRequested`; Inventory→Sales: `StockReserved`/`StockRejected`/`StockReleased`; Sales & Inventory → AuditLog: `AuditChanged` qua `EventEnvelope`).
- **Forbidden dependencies**: Sales.*, Inventory.*, AuditLog.*, EF Core, MediatR, KafkaFlow — project này chỉ có record/enum/interface thuần C#, không package ngoài.
- **Notes**: đã xoá `PagedResult<T>` (dead code trùng lặp — không được reference bởi bất kỳ project nào; `PagedResult<T>` thật nằm ở `Sales.Application/DTOs/PagedResult.cs` và không phải integration event nên không thuộc Contracts). `EventEnvelope.Create<T>(...)` (factory có logic `JsonSerializer`) đã được tách ra khỏi record thành `EventEnvelopeFactory` **nội bộ riêng ở từng Infrastructure producer** (`Sales.Infrastructure/Kafka/EventEnvelopeFactory.cs`, `Inventory.Infrastructure/Kafka/EventEnvelopeFactory.cs`) — giữ Contracts thuần data, không phụ thuộc logic serialize. AuditLog không cần factory này vì chỉ consume/deserialize, không tạo `EventEnvelope`.

## Shared/BuildingBlocks.Observability

- **Required base types**: `SerilogBootstrap.ConfigureSharedSinks(...)` (✅ — 1 extension method dùng chung bởi Sales.Api, Inventory.Api, AuditLog.Worker để cấu hình Serilog: Console + Seq + OTLP, cùng enricher `Service`/`Environment`; thay cho 3 block `UseSerilog(...)` copy/paste riêng trước đây).
- **Forbidden dependencies**: không có project nghiệp vụ nào (`Sales.*`, `Inventory.*`, `AuditLog.*`) — chỉ chứa cross-cutting logging bootstrap thuần Serilog + `Microsoft.Extensions.Configuration.Abstractions`.
- **Notes**: chi tiết đầy đủ ở `docs/Seqlog-usage-guide.md` mục 4 và `docs/open-telemetry-usage-guide.md` mục 6 (nhánh log OTLP). Chưa có test project riêng — hiện chỉ 1 extension method thuần cấu hình, chưa có logic đủ phức tạp để cần unit test.

## Shared/BuildingBlocks.Web

- **Required base types**: `RequestObservabilityMiddleware` (✅ — middleware HTTP dùng chung bởi Sales.Api và Inventory.Api, gộp lại từ 2 middleware riêng cũ `CorrelationLoggingMiddleware`/`HttpLoggingMiddleware`; enrich `RequestId`/`CorrelationId`/`TraceId`/`UserId`/`Url`/`Route`/body request-response đã mask dữ liệu nhạy cảm), `RequestLoggingDefaults.Configure` (✅ — tùy chỉnh level của `UseSerilogRequestLogging`, hạ `/health`/`/hangfire` xuống Debug để không làm ồn log Information).
- **Forbidden dependencies**: `Sales.*`, `Inventory.*`, `AuditLog.*` — chỉ chứa middleware ASP.NET Core thuần, không biết gì về domain/CQRS của service nào.
- **Notes**: `AuditLog.Worker` không reference project này (không có HTTP). Chi tiết đầy đủ ở `docs/Seqlog-usage-guide.md` mục 6, 8.

## Docker

- `docker/docker-compose.yml`: postgres, redis, mongo, kafka, seq, elasticsearch/kibana/apm-server, otel-collector, sales-api, inventory-api, audit-worker — ✅ đầy đủ cho toàn bộ pattern được yêu cầu (PostgreSQL, MongoDB, Kafka, observability).
- **Notes**: không có thay đổi nào cần thiết ở lớp Docker trong đợt review này — không có service mới được thêm, không có port/env mới cần khai báo.

## Tests

- `Sales.Domain.Tests`, `Sales.Application.Tests`, `Sales.Infrastructure.Tests`, `Sales.Architecture.Tests`, `Inventory.Tests`, `Inventory.Infrastructure.Tests`, `AuditLog.Tests` — ✅ đủ 1 test project / layer cho Sales, và cân đối cho Inventory/AuditLog theo quy mô nhỏ hơn.
- `Sales.Architecture.Tests` dùng NetArchTest để enforce dependency rule tĩnh — đây là cơ chế chính giữ cho rule ở mục 5 (`CODING_RULES.md`) không bị vi phạm âm thầm theo thời gian.
- **Forbidden**: reliability test (`*ReliabilityTests.cs`) chỉ chạy khi `RUN_RELIABILITY_TESTS=true` (cần Postgres/Mongo thật) — không được để mặc định chạy trong CI thường (sẽ fail nếu không có DB).
- **Notes**: chưa có test project riêng cho `BuildingBlocks.Contracts` — hiện tại project này chỉ chứa record/enum thuần không có logic, nên chưa cần; nếu sau này thêm logic (mapping, validation) vào Contracts thì cần bổ sung test.
