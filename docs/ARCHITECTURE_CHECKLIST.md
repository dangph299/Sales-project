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

- **Required folders**: `Persistence/{DbContexts,Configurations,Migrations,Inbox}/`, `Repositories/`, `Kafka/`, `Hangfire/`, `ExternalServices/`, `UnitOfWork/` — ✅ đã có đủ. `Persistence/Outbox/` và `Kafka/EventEnvelopeFactory.cs`/`Kafka/KafkaOutboxPublisher.cs`/`Observability/SalesActivitySource.cs` đã bị xoá — chuyển sang `Shared/BuildingBlocks.Infrastructure` (xem mục riêng bên dưới), không phải bị mất nhầm.
- **Required base types**: `SalesDbContext` (✅, `OnModelCreating` giờ áp dụng `ApplyConfigurationsFromAssembly` cho **cả** assembly của chính nó **lẫn** assembly của `BuildingBlocks.Infrastructure` để pick up `OutboxMessageConfiguration` dùng chung), 7 `IEntityTypeConfiguration<T>` (✅, còn `InboxMessage` — `OutboxMessage`'s config đã chuyển sang `BuildingBlocks.Infrastructure`), `Repository<T>` + 2 repository implementation cụ thể (`ProductRepository`, `OrderRepository`, ✅), `InboxMessage` (✅, giữ nguyên tại Sales — có cột `Consumer` mà Inventory không có, cố ý **không** unify với `Inventory.Infrastructure.InboxRow`), `UnitOfWork` (✅ — wrapper mỏng implement `IUnitOfWork`, delegate thẳng `SalesDbContext.SaveChangesAsync`), `SalesOutboxPublisher` (✅, `BackgroundService` polling outbox — cố ý **không** merge với `InventoryOutboxPublisher` thành 1 generic base class, xem `BuildingBlocks.Infrastructure`), `AddSalesInfrastructure()` DI extension (✅, tách private extension theo nhóm repositories/read services/context/cache/messaging; `AddSalesMessaging` giờ đăng ký `ActivitySource` singleton + `IOutboxPublisher` qua factory lambda truyền `"sales-outbox"` producer name).
- **Optional types**: Options class (`KafkaOptions`, `RedisOptions`...) — **chưa tạo**; hiện đọc trực tiếp qua `IConfiguration` (`GetConnectionString`, `configuration["Kafka:Brokers"]`). Để dành cho sub-project sau nếu muốn strongly-typed config toàn bộ.
- **Forbidden dependencies**: không có (Infrastructure được phép dùng mọi framework).
- **Notes**: `UnitOfWork/UnitOfWork.cs` tách khỏi `SalesDbContext` để `IUnitOfWork` không lộ toàn bộ surface của DbContext ra Application — không đổi hành vi (vẫn gọi đúng `SalesDbContext.SaveChangesAsync`, nơi có logic map domain event → outbox). Đã fix 1 bug thật trong lúc thêm `IOutboxPublisher`: đăng ký Scoped nhưng bị Singleton `SalesOutboxPublisher` (BackgroundService) inject trực tiếp → đổi sang `AddSingleton` (xem báo cáo). **Cập nhật 2026-07-10**: `OutboxMessage`, `IOutboxPublisher`/`KafkaOutboxPublisher`, `EventEnvelopeFactory`, và `SalesActivitySource` (đổi thành DI-injected `ActivitySource` trực tiếp, không còn static wrapper class) đã chuyển sang `Shared/BuildingBlocks.Infrastructure`/`Shared/BuildingBlocks.Observability` dùng chung với Inventory — xem mục "Shared/BuildingBlocks.Infrastructure" và bảng Refactor Review Notes bên dưới.

## Refactor Review Notes

| Area | Duplicate Pattern | Current Problem | Proposed Abstraction | Action | Risk |
|---|---|---|---|---|---|
| Sales repositories | CRUD methods per aggregate | Copy/paste risk when adding aggregate repositories | `IRepository<T>` + `Repository<T>` | Kept generic CRUD; specific repos only for `GetBySkuAsync` and `GetWithLinesAsync` | Low |
| Order command support | Loading products one by one | Repeated repository calls and possible N+1 query pattern | `GetByIdsAsync` bulk load via generic repository | `Materialize` now loads distinct product ids once | Low |
| Validators | Id/version/phone/order-line uniqueness rules | Same FluentValidation expressions repeated | `CommonValidationRules` extension methods | Applied to Customer/Order/Product validators where duplicated | Low |
| Read services | `page = Math.Max...` / `Math.Clamp...` | Paging bounds repeated in each search | `Paging.Normalize(...)` | Applied to Product/Customer/Order read services | Low |
| DI registration | One growing Infrastructure method | Harder to add feature-specific registrations cleanly | Private `IServiceCollection` extension methods | Split repositories/read services/context/cache/messaging groups | Low |
| Cross-cutting handlers | Validation/logging | Already handled consistently | MediatR pipeline behaviors | No new behavior added | None |
| Sales/Inventory Kafka infra | `EventEnvelopeFactory`, Outbox entity/`IOutboxPublisher`/`KafkaOutboxPublisher`, `ActivitySource` wrapper, Outbox/Inbox metrics, consumer-handler tracing+error-classification boilerplate — each duplicated near-verbatim between the two services | Fixing/extending outbox or Kafka-consumer plumbing meant editing two copies and risked them drifting | `Shared/BuildingBlocks.Infrastructure` (`OutboxMessage`/`IOutboxPublisher`/`KafkaOutboxPublisher`/`EventEnvelopeFactory`/`PostgresExceptions`/`KafkaConsumerActivity`) + `Shared/BuildingBlocks.Observability` (`OutboxMetrics`/`InboxMetrics`/`ObservabilityNames`) | Merged 2026-07-10 per `docs/superpowers/specs/2026-07-09-shared-infrastructure-refactor-design.md`; outbox-polling `BackgroundService` and consumer `HandleCore` dispatch deliberately left unmerged (see notes above) | Low (build+full test suite green, EF migration probe confirmed no schema change, reviewed) |

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

- **Required folders**: `Persistence/{DbContexts,Configurations,Migrations,Inbox}/`, `Kafka/`, `Observability/`, `Services/` — ✅ đã có đủ. `Persistence/Outbox/` đã bị xoá (entity chuyển sang `BuildingBlocks.Infrastructure`), `Observability/InventoryActivitySource.cs` đã bị xoá (thay bằng DI-injected `ActivitySource`) — không phải bị mất nhầm.
- **Required base types**: `InventoryDbContext` (✅, `OnModelCreating` áp dụng `ApplyConfigurationsFromAssembly` cho **cả** assembly của chính nó **lẫn** assembly của `BuildingBlocks.Infrastructure`), 5 `IEntityTypeConfiguration<T>` (✅, còn `InboxRow` — `OutboxRow`'s config đã đổi tên thành `OutboxMessageConfiguration` và chuyển sang `BuildingBlocks.Infrastructure`), `InboxRow` (✅, giữ nguyên tại Inventory — **không** có cột `Consumer` như Sales' `InboxMessage`, cố ý không unify), `InventoryOutboxPublisher` (✅, `BackgroundService` polling outbox — cố ý **không** merge với `SalesOutboxPublisher`, chỉ đổi type tham chiếu từ `OutboxRow` cục bộ sang `OutboxMessage` dùng chung), `AddInventoryInfrastructure()` DI extension (✅, giờ đăng ký `ActivitySource` singleton + `IOutboxPublisher` qua factory lambda truyền `"inventory-outbox"` producer name).
- **Optional types**: Repository implementation — không có (không cần, vì Domain không có repository interface — xem Inventory.Domain).
- **Forbidden dependencies**: không có.
- **Notes**: cùng bug Scoped/Singleton như Sales đã được phát hiện và fix ở đây trước (do `dotnet ef migrations add` build full host và validate DI graph) — root cause chung, fix chung (`AddSingleton<IOutboxPublisher, KafkaOutboxPublisher>()`). **Cập nhật 2026-07-10**: `OutboxRow` đổi tên thành `OutboxMessage` (canonical name của Sales, theo quyết định chủ dự án) và chuyển sang `BuildingBlocks.Infrastructure` dùng chung với Sales — bảng `outbox_messages` không đổi (xác nhận qua migration probe rỗng).

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
- **Notes**: đã xoá `PagedResult<T>` (dead code trùng lặp — không được reference bởi bất kỳ project nào; `PagedResult<T>` thật nằm ở `Sales.Application/DTOs/PagedResult.cs` và không phải integration event nên không thuộc Contracts). `EventEnvelope.Create<T>(...)` (factory có logic `JsonSerializer`) đã được tách ra khỏi record thành `EventEnvelopeFactory` — giữ Contracts thuần data, không phụ thuộc logic serialize. **Cập nhật 2026-07-10**: `EventEnvelopeFactory` từng có 2 bản gần như y hệt (`Sales.Infrastructure/Kafka/EventEnvelopeFactory.cs`, `Inventory.Infrastructure/Kafka/EventEnvelopeFactory.cs`) nay đã hợp nhất thành 1 bản duy nhất tại `Shared/BuildingBlocks.Infrastructure/Messaging/EventEnvelopeFactory.cs` — xem mục "Shared/BuildingBlocks.Infrastructure". Vẫn không đặt tại `BuildingBlocks.Contracts` (project này vẫn phải thuần data, không phụ thuộc EF Core/KafkaFlow theo `CODING_RULES.md`). AuditLog không cần factory này vì chỉ consume/deserialize, không tạo `EventEnvelope`.

## Shared/BuildingBlocks.Observability

- **Required folders**: `Metrics/` (✅ mới — `OutboxMetrics`, `InboxMetrics`).
- **Required base types**: `SerilogBootstrap.ConfigureSharedSinks(...)` (✅ — 1 extension method dùng chung bởi Sales.Api, Inventory.Api, AuditLog.Worker để cấu hình Serilog: Console + Seq + OTLP, cùng enricher `Service`/`Environment`; thay cho 3 block `UseSerilog(...)` copy/paste riêng trước đây), `OutboxMetrics` (✅ mới — nhận `(string meterName, string prefix)`, expose `Published`/`Failed`/`DeadLettered` counter + `SetSnapshot(backlog, deadLetters)` cho 2 gauge), `InboxMetrics` (✅ mới — nhận `(string meterName, string prefix)`, expose `Duplicate`/`Processed` counter), `ObservabilityNames` (✅ mới — hằng số tên tracing source `SalesKafka`/`InventoryKafka`, dùng chung giữa DI registration của `ActivitySource` và `AddSource(...)` ở từng Api, tránh 2 nguồn sự thật lệch nhau âm thầm làm rớt span không báo lỗi).
- **Forbidden dependencies**: không có project nghiệp vụ nào (`Sales.*`, `Inventory.*`, `AuditLog.*`) — chỉ chứa cross-cutting logging/metrics bootstrap thuần Serilog + `System.Diagnostics.Metrics` (BCL) + `Microsoft.Extensions.Configuration.Abstractions`.
- **Notes**: chi tiết đầy đủ ở `docs/Seqlog-usage-guide.md` mục 4 và `docs/open-telemetry-usage-guide.md` mục 6 (nhánh log OTLP). Chưa có test project riêng — hiện chỉ 1 extension method thuần cấu hình, chưa có logic đủ phức tạp để cần unit test. **Cập nhật 2026-07-10**: `SalesMetrics`/`InventoryMetrics` (mỗi service, nằm tại `*.Infrastructure/Observability/`) giữ nguyên `internal static class` với đúng tên/type member cũ (zero call-site churn) nhưng nội bộ giờ delegate sang `OutboxMetrics`/`InboxMetrics` — `InventoryMetrics` vẫn giữ riêng 2 counter `ReservationRejected`/`ReservationReserved` (không phải outbox/inbox, không unify).

## Shared/BuildingBlocks.Infrastructure

- **Required folders**: `Outbox/`, `Messaging/`, `Tracing/`, `Exceptions/` — ✅ (project mới, tạo ngày 2026-07-10).
- **Required base types**: `OutboxMessage` (✅ — hợp nhất từ `Sales.Infrastructure.OutboxMessage` + `Inventory.Infrastructure.OutboxRow`, cùng 11 property + `MaxAttempts` const; mỗi service vẫn tự khai `IEntityTypeConfiguration<OutboxMessage>`/`ToTable("outbox_messages")` riêng trong project của mình — 2 database độc lập, không phải duplication), `IOutboxPublisher` (✅ — hợp nhất, không còn generic theo từng service), `KafkaOutboxPublisher` (✅ — hợp nhất, nhận `ActivitySource` + `producerName` qua constructor thay vì hardcode/static class riêng), `EventEnvelopeFactory` (✅ — hợp nhất từ 2 bản y hệt), `PostgresExceptions.IsUniqueViolation` (✅ mới — thay 2 bản `private static bool IsUniqueViolation(...)` y hệt trong `SalesIntegrationEventHandler`/`InventoryEventHandler`), `KafkaConsumerActivity.Start` (✅ mới — gói đoạn mở tracing span consumer-side, thay đoạn code lặp y hệt ở 2 handler).
- **Optional types / Cố ý KHÔNG làm**: **không** merge `SalesOutboxPublisher`/`InventoryOutboxPublisher` (BackgroundService polling outbox) thành 1 generic base class — quyết định chủ dự án, chấp nhận ~100 dòng trùng lặp để không tăng rủi ro ở phần code nhạy cảm nhất về độ tin cậy publish message. **Không** tạo shared base class cho `HandleCore` của 2 Kafka consumer handler — logic dispatch nghiệp vụ khác hẳn nhau (Sales: order status transition; Inventory: stock reservation), chỉ phần thuần kỹ thuật (`IsUniqueViolation`, mở tracing span) được tách.
- **Forbidden dependencies**: chỉ được reference bởi `Sales.Infrastructure`/`Inventory.Infrastructure`, không được reference bởi Domain/Application/Api của bất kỳ service nào — enforce một phần bằng `Sales.Architecture.Tests`. Bản thân project này reference `BuildingBlocks.Contracts` + package hạ tầng (`Microsoft.EntityFrameworkCore`, `Npgsql` — driver thuần, không phải `Npgsql.EntityFrameworkCore.PostgreSQL`, vì chỉ cần `PostgresException`/`PostgresErrorCodes`, `KafkaFlow`, `Microsoft.Extensions.Logging.Abstractions`).
- **Notes**: chi tiết đầy đủ, bao gồm lý do từng quyết định phạm vi, ở `docs/superpowers/specs/2026-07-09-shared-infrastructure-refactor-design.md`. Đã xác nhận qua `dotnet ef migrations add ZZ_Probe` (Up/Down rỗng) rằng việc hợp nhất `OutboxMessage` không đổi schema `outbox_messages` ở cả 2 database.

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
