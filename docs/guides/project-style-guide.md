# Project Style Guide - Sales Management Backend (.NET 10)

> Quy chuẩn kiến trúc, convention, pattern và coding style cho solution hiện tại.
> Đọc file này trước khi viết hoặc refactor code. Nếu có xung đột, ưu tiên codebase hiện tại và `docs/architecture.md`.

---

## 1. Architecture Overview

Solution hiện tại là .NET 10 sample gồm:

- Sales bounded context: API + Application + Domain + Infrastructure.
- Inventory bounded context: API + Application + Domain + Infrastructure.
- AuditLog bounded context: Worker + Infrastructure.
- Shared building blocks: contracts, observability, web middleware.
- Angular test client để test thủ công API.

Các pattern chính đang dùng:

- DDD cho aggregate, value object, domain event và invariant.
- Clean Architecture theo hướng dependency vào trong.
- CQRS + MediatR trong Sales Application.
- Repository + Unit of Work cho command-side aggregate persistence.
- Read-service ports cho query-side EF projections.
- Specification Pattern cho filter có thể tái sử dụng.
- Outbox/Inbox Pattern cho Kafka at-least-once delivery và idempotency.
- KafkaFlow cho integration event publish/consume.
- Hangfire cho scheduled/maintenance jobs của Sales.
- EF Core + PostgreSQL cho Sales và Inventory.
- MongoDB cho AuditLog.
- Redis cho cache-aside và distributed lock.
- Serilog + Seq + OpenTelemetry cho observability.

Luồng confirm order chính:

```text
Client
  -> Sales.Api controller
  -> MediatR command/query
  -> Sales.Application handler
  -> Sales.Domain aggregate
  -> Sales.Infrastructure repository / UnitOfWork
  -> PostgreSQL + Sales outbox
  -> SalesOutboxPublisher hosted service
  -> KafkaFlow
  -> InventoryEventHandler + Inventory inbox
  -> Inventory.Domain reservation
  -> Inventory PostgreSQL + Inventory outbox
  -> KafkaFlow
  -> SalesIntegrationEventHandler + Sales inbox
  -> Sales order state update
  -> AuditLog.Worker consumes integration events
  -> MongoDB audit documents
```

---

## 2. Dependency Rules

Dependency direction:

```text
Api/Worker -> Infrastructure -> Application -> Domain
```

Project rules:

- `*.Domain` không reference EF Core, KafkaFlow, Redis, HTTP, MediatR, Infrastructure, Api/Worker hoặc bounded context khác.
- `*.Application` reference Domain và định nghĩa ports/interfaces. Không reference Infrastructure hoặc Api.
- `*.Infrastructure` implement Application/Domain ports và được phép dùng EF Core, KafkaFlow, Redis, MongoDB, external libraries.
- `*.Api` và `*.Worker` là composition root/host. Không chứa business rule.
- Cross-service contracts đặt trong `src/Shared/BuildingBlocks.Contracts`, không tạo reference trực tiếp giữa Sales và Inventory.
- Shared projects không được chứa domain model của bounded context.

Project references hiện tại:

```text
Sales.Api
  -> Sales.Application
  -> Sales.Infrastructure
  -> BuildingBlocks.Observability
  -> BuildingBlocks.Web

Sales.Infrastructure
  -> Sales.Application
  -> Sales.Domain
  -> BuildingBlocks.Contracts

Sales.Application
  -> Sales.Domain

Inventory.Api
  -> Inventory.Application
  -> Inventory.Infrastructure
  -> BuildingBlocks.Observability
  -> BuildingBlocks.Web

Inventory.Infrastructure
  -> Inventory.Application
  -> Inventory.Domain
  -> BuildingBlocks.Contracts

AuditLog.Worker
  -> AuditLog.Infrastructure
  -> BuildingBlocks.Observability

AuditLog.Infrastructure
  -> BuildingBlocks.Contracts
```

---

## 3. Solution Structure

```text
src/
├── Services/
│   ├── Sales/
│   │   ├── Sales.Api/
│   │   ├── Sales.Application/
│   │   ├── Sales.Domain/
│   │   └── Sales.Infrastructure/
│   ├── Inventory/
│   │   ├── Inventory.Api/
│   │   ├── Inventory.Application/
│   │   ├── Inventory.Domain/
│   │   └── Inventory.Infrastructure/
│   └── AuditLog/
│       ├── AuditLog.Worker/
│       └── AuditLog.Infrastructure/
├── Shared/
│   ├── BuildingBlocks.Contracts/
│   ├── BuildingBlocks.Observability/
│   └── BuildingBlocks.Web/
└── Web/
    └── Sales.TestClient/

tests/
├── Sales.Domain.Tests/
├── Sales.Application.Tests/
├── Sales.Infrastructure.Tests/
├── Sales.Architecture.Tests/
├── Inventory.Tests/
├── Inventory.Infrastructure.Tests/
├── AuditLog.Tests/
└── Playwright/

docker/
├── docker-compose.yml
├── seed/
└── observability config files
```

Không thêm project `*.Contracts` riêng cho từng service nếu contract đó dùng cross-service; dùng `BuildingBlocks.Contracts`.

---

## 4. Domain Layer Convention

Base type dùng chung (`AggregateRoot<TId>`, `Entity<TId>`/`IEntity<TId>`, `IAggregateRoot`, `IDomainEvent`/`DomainEvent`, `DomainException`) **không còn nằm trong `Sales.Domain`** — đã chuyển sang `Shared/BuildingBlocks.Domain/Abstractions/` (+ `Exceptions/`) từ 2026-07-10 để `Inventory.Domain` cũng dùng chung được. `Sales.Domain`/`Inventory.Domain` reference project này qua `global using BuildingBlocks.Domain;`.

Sales.Domain hiện tại:

```text
Sales.Domain/
├── Aggregates/
│   ├── Customer.cs
│   ├── Order.cs
│   └── Product.cs
├── Entities/
│   └── OrderLine.cs
├── Events/
│   ├── Customers/
│   ├── Orders/
│   └── Products/
├── Exceptions/       (.gitkeep — chưa có DomainException con riêng của Sales)
├── Repositories/
├── Services/
│   └── Specifications/
└── ValueObjects/
```

Inventory.Domain hiện tại:

```text
Inventory.Domain/
├── Aggregates/
│   └── Reservation.cs   (AggregateRoot<Guid>)
├── Entities/
│   ├── InventoryItem.cs (IEntity<Guid>)
│   └── ReservationLine.cs
└── ValueObjects/
```

Shared/BuildingBlocks.Domain hiện tại:

```text
Shared/BuildingBlocks.Domain/
├── Abstractions/
│   ├── AggregateRoot.cs
│   ├── DomainEvent.cs
│   ├── Entity.cs
│   ├── IAggregateRoot.cs
│   ├── IDomainEvent.cs
│   └── IEntity.cs
└── Exceptions/
    └── DomainException.cs
```

Rules:

- Aggregate root bảo vệ invariant và là nơi chính thay đổi state.
- Entity không expose setter public tùy tiện.
- Value object immutable, validate input ở factory/constructor.
- Domain event chỉ mô tả sự kiện nghiệp vụ; không chứa topic Kafka, header, serializer, EF hoặc transport concern.
- Không inject repository/service infrastructure vào entity hoặc aggregate.
- Không dùng DTO/API request trong Domain.
- Không dùng tuple để biểu diễn business data có ý nghĩa nghiệp vụ.
- Domain exception dùng cho invariant violation; Application exception dùng cho orchestration như not found/conflict.

---

## 5. Application Layer Convention

Sales.Application hiện tại:

```text
Sales.Application/
├── Commands/
│   ├── Customers/
│   ├── Orders/
│   └── Products/
├── Queries/
│   ├── Customers/
│   ├── Orders/
│   └── Products/
├── DTOs/
├── Interfaces/
├── Services/            (ConflictException, NotFoundException, SalesApplicationExceptionClassifier)
├── Validators/
├── Common/
└── DependencyInjection.cs
```

`Services/Behaviors/` không còn tồn tại — 3 pipeline behavior (`ErrorLoggingBehavior`, `LoggingBehavior`, `ValidationBehavior`) đã chuyển sang `Shared/BuildingBlocks.Application/Behaviors/` từ 2026-07-10 (dùng chung nếu sau này có CQRS service khác). `IUnitOfWork` và `PagedResult`/`Paging` cũng chuyển sang `Shared/BuildingBlocks.Application/Persistence/` và `Pagination/` — không còn trong `Sales.Application/Interfaces/` hay `DTOs/`.

Rules:

- Command thay đổi state; Query chỉ đọc dữ liệu.
- Request MediatR hiện tại dùng tên ngắn theo use case, ví dụ `CreateOrder`, `ConfirmOrder`, `SearchOrders`, không dùng hậu tố `Command`/`Query` bắt buộc.
- Handler đặt cùng feature folder trong `Commands/<Area>` hoặc `Queries/<Area>`, ví dụ `CreateOrderHandler`.
- Validator đặt trong `Validators/<Area>`, ví dụ `CreateOrderValidator`.
- DTO/read models đặt trong `DTOs/<Area>`.
- Ports đặt trong `Interfaces`, ví dụ `IOrderReadService`, `IProductCache`, `IExecutionContext`. `IUnitOfWork` là port dùng chung, nay nằm ở `BuildingBlocks.Application` thay vì `Sales.Application/Interfaces/`.
- Handler không chứa domain rule phức tạp; gọi aggregate/domain service để xử lý nghiệp vụ.
- Handler command không gọi `DbContext` trực tiếp; dùng repository/read port/unit of work.
- Query handler gọi read-service port; EF projection nằm trong Infrastructure.
- Validation dùng FluentValidation.
- Mapping hiện tại dùng extension/mapping trong `DtoMapping.cs` và Mapster package nếu phù hợp; không trả entity ra API.
- Không publish Kafka trực tiếp trong handler; thay đổi business data và outbox phải đi cùng transaction.
- Exception nào được coi là "expected rejection" (log Warning thay vì Error) khai báo qua `IApplicationExceptionClassifier`; mỗi service extend `BuildingBlocks.Application.DefaultApplicationExceptionClassifier` thay vì sửa thẳng `ErrorLoggingBehavior`, ví dụ `SalesApplicationExceptionClassifier` thêm `NotFoundException`/`ConflictException`.

MediatR pipeline hiện tại (đăng ký qua `BuildingBlocks.Application.DependencyInjection.AddApplicationBuildingBlocks()`, được `Sales.Application.AddSalesApplication()` gọi lại):

```text
ErrorLoggingBehavior
LoggingBehavior
ValidationBehavior
```

Nếu thêm behavior mới dùng chung cho mọi CQRS service, đăng ký trong `Shared/BuildingBlocks.Application/DependencyInjection.cs`; nếu chỉ riêng Sales cần, đăng ký trong `Sales.Application/DependencyInjection.cs` sau lời gọi `AddApplicationBuildingBlocks()` và cân nhắc thứ tự wrapping.

---

## 6. CQRS + MediatR Naming

Convention hiện tại:

```text
CreateOrder
CreateOrderHandler
CreateOrderValidator
OrderDto

SearchOrders
SearchOrdersHandler
PagedResult<OrderDto>  (BuildingBlocks.Application.PagedResult<T>)
```

Rules:

- Request nên là `sealed record` và implement `IRequest<TResponse>`.
- Handler nên là `sealed class <RequestName>Handler : IRequestHandler<...>`.
- Handler trả DTO/response rõ ràng, không trả aggregate/entity.
- Không dùng tuple làm response.
- Luôn truyền `CancellationToken`.
- Log event nghiệp vụ quan trọng bằng structured logging, không log sensitive payload.

---

## 7. Repository + Unit Of Work

Domain khai báo repository contract:

```text
Sales.Domain/Repositories/
├── IRepository.cs
├── IOrderRepository.cs
└── IProductRepository.cs
```

Infrastructure implement:

```text
Sales.Infrastructure/
├── Repositories/
│   ├── Repository.cs
│   ├── OrderRepository.cs
│   └── ProductRepository.cs
└── UnitOfWork/
    └── UnitOfWork.cs
```

`IUnitOfWork` là interface dùng chung, khai báo ở `Shared/BuildingBlocks.Application/Persistence/IUnitOfWork.cs` — không phải một file trong `Sales.Domain`/`Sales.Application`.

Rules:

- Repository dùng cho aggregate/command-side persistence, không tạo repository cho mọi table nếu không cần.
- Repository không expose `DbSet` hoặc `IQueryable`.
- Repository không commit transaction và không publish event.
- Command handler gọi `IUnitOfWork.SaveChangesAsync(ct)` sau khi thay đổi aggregate.
- Optimistic concurrency conflict phải được convert thành lỗi application/API phù hợp.

---

## 8. Specification Pattern

Project hiện tại có specification base trong Domain:

```text
Sales.Domain/Services/Specifications/
├── ISpecification.cs
└── Specification.cs
```

Và EF/query specifications trong Infrastructure:

```text
Sales.Infrastructure/Persistence/Specifications/
├── OrderCreatedFromSpecification.cs
├── OrderCreatedToSpecification.cs
└── OrderCustomerMatchesSpecification.cs
```

Rules:

- Domain specification dùng cho business rule thuần domain.
- Infrastructure specification dùng cho EF/query filter.
- Không nhồi filter phức tạp lặp lại trực tiếp trong handler/read service.
- Tên specification phải mô tả rule/filter rõ ràng.

---

## 9. EF Core + PostgreSQL

Sales persistence:

```text
Sales.Infrastructure/Persistence/
├── DbContexts/SalesDbContext.cs
├── Configurations/
├── Migrations/
├── Inbox/
├── Outbox/
├── CustomerReadService.cs
├── OrderReadService.cs
├── ProductReadService.cs
└── CachedProductReadService.cs
```

Inventory persistence:

```text
Inventory.Infrastructure/Persistence/
├── DbContexts/InventoryDbContext.cs
├── Configurations/
├── Migrations/
├── Inbox/
└── Outbox/
```

Rules:

- Code First bằng EF Core migrations.
- Entity configuration dùng `IEntityTypeConfiguration<T>`.
- Không cấu hình entity lộn xộn trong DbContext khi có thể tách configuration.
- Read-only query dùng `AsNoTracking()`.
- Không expose `IQueryable` ra ngoài Infrastructure.
- Không dùng lazy loading.
- Concurrency token/version phải được giữ khi update order/edit/confirm flow.
- Migration không được drop data/schema tùy tiện.

---

## 10. Outbox Pattern

Mục tiêu: DB commit thành công thì integration event không bị mất.

Sales:

```text
Sales.Infrastructure/Persistence/Outbox/
├── OutboxMessage.cs
└── IOutboxPublisher.cs

Sales.Infrastructure/Kafka/
├── DomainEventMapper.cs
├── EventEnvelopeFactory.cs
├── KafkaOutboxPublisher.cs
└── SalesOutboxPublisher.cs
```

Inventory:

```text
Inventory.Infrastructure/Persistence/Outbox/
├── OutboxRow.cs
└── IOutboxPublisher.cs

Inventory.Infrastructure/Kafka/
├── EventEnvelopeFactory.cs
├── KafkaOutboxPublisher.cs
└── InventoryOutboxPublisher.cs
```

Rules:

- Domain event được map thành integration event trong Infrastructure.
- Integration event được lưu vào outbox cùng transaction với business data.
- Hosted service publisher đọc outbox chưa xử lý và publish qua KafkaFlow.
- Publish thành công thì mark processed.
- Publish lỗi thì lưu lỗi/retry metadata theo schema hiện có.
- Publisher phải retry-safe và idempotent.
- Không publish Kafka trực tiếp trước khi transaction chính commit.

---

## 11. Inbox Pattern

Mục tiêu: consumer xử lý event idempotent, không tạo duplicate side-effect.

Sales:

```text
Sales.Infrastructure/Persistence/Inbox/
└── InboxMessage.cs

Sales.Infrastructure/Kafka/
└── SalesIntegrationEventHandler.cs
```

Inventory:

```text
Inventory.Infrastructure/Persistence/Inbox/
└── InboxRow.cs

Inventory.Infrastructure/Kafka/
└── InventoryEventHandler.cs
```

Rules:

- Mỗi Kafka message phải có stable `MessageId`/`EventId`.
- Consumer check inbox trước khi xử lý side effect.
- Nếu message đã xử lý thì skip an toàn.
- Nếu chưa xử lý thì insert inbox và xử lý nghiệp vụ trong transaction phù hợp.
- Không dựa riêng vào Kafka offset để đảm bảo idempotency nghiệp vụ.

---

## 12. KafkaFlow + Contracts

Contracts chung nằm trong:

```text
src/Shared/BuildingBlocks.Contracts/
├── IntegrationEvents/
│   ├── Common/
│   ├── Inventory/
│   └── Sales/
└── Messaging/
    ├── ContractVersions.cs
    ├── KafkaConsumerGroups.cs
    ├── KafkaTopics.cs
    ├── MessageHeaders.cs
    └── TraceContextParser.cs
```

Rules:

- Topic name và consumer group lấy từ `KafkaTopics` và `KafkaConsumerGroups`; không hard-code rải rác.
- Integration event contract đặt trong `BuildingBlocks.Contracts`, không đặt trong Domain.
- Event envelope/header phải giữ message id, correlation/trace context, event type, version và occurred time theo contract hiện có.
- Không publish Domain Event trực tiếp ra Kafka.
- Consumer không chứa business logic lớn; translate event rồi gọi domain/application/service phù hợp.
- Nếu contract thay đổi breaking, thêm version thay vì sửa silent contract đang dùng.

---

## 13. Hangfire, Redis, Cache

Sales.Api dùng Hangfire cho dashboard/job host; Sales.Infrastructure có:

```text
Sales.Infrastructure/Hangfire/MaintenanceJobs.cs
Sales.Infrastructure/ExternalServices/CacheService.cs
Sales.Infrastructure/ExternalServices/ProductCache.cs
```

Rules:

- Job phải retry-safe và idempotent.
- Job không nhận object lớn; nhận id/filter nhỏ.
- Không enqueue job phụ thuộc dữ liệu chưa commit.
- Redis cache dùng qua Application ports, không gọi trực tiếp từ Application/Domain.
- Cache-aside phải có invalidation rõ ràng khi dữ liệu nguồn thay đổi.
- Distributed lock hỗ trợ coordination, không thay thế correctness ở database.

---

## 14. MongoDB AuditLog

AuditLog hiện tại:

```text
AuditLog.Infrastructure/
├── Mongo/
│   ├── AuditDocument.cs
│   ├── AuditEventHandler.cs
│   ├── IAuditWriter.cs
│   └── MongoAuditWriter.cs
├── Options/
└── Observability/

AuditLog.Worker/
├── Hosting/
│   ├── KafkaBusService.cs
│   └── MongoStartupService.cs
├── DependencyInjection.cs
└── Program.cs
```

Rules:

- AuditLog nhận integration event từ Kafka và lưu MongoDB.
- Audit document phải idempotent theo unique event id.
- Audit data nên append-only; không update/delete audit tùy tiện.
- Không lưu password/token/raw sensitive payload.
- Worker là host lifecycle, không chứa mapping/persistence logic lớn.

---

## 15. API Layer

Sales.Api hiện tại:

```text
Sales.Api/
├── Controllers/
├── Extensions/
├── Filters/
├── Middleware/
├── Models/Requests/
└── Program.cs
```

Inventory.Api hiện tại:

```text
Inventory.Api/
├── Controllers/
├── Extensions/
├── AdjustStockRequest.cs
└── Program.cs
```

Rules:

- Dùng ASP.NET Core MVC Controllers cho HTTP APIs; không thêm Minimal API endpoint groups cho business APIs.
- Controller chỉ nhận request, xử lý transport concern như auth/ETag, gọi MediatR/service và trả response.
- Không inject repository hoặc DbContext vào controller.
- Không chứa business logic trong controller.
- Với Sales controllers/API-facing classes, dùng constructor explicit với private readonly field; không dùng C# primary constructor.
- Request models đặt trong `Models/Requests` hoặc gần API khi chỉ dùng cho transport.
- API không trả entity/aggregate trực tiếp.
- Error handling dùng middleware/problem details theo pattern hiện có.

---

## 16. Shared Building Blocks

```text
BuildingBlocks.Contracts
  versioned integration event envelopes/contracts and messaging constants

BuildingBlocks.Observability
  SerilogBootstrap.ConfigureSharedSinks and shared sink policy

BuildingBlocks.Web
  JWT auth helpers, OpenAPI helpers, request logging/observability middleware
```

Rules:

- Chỉ đưa code vào Shared khi có ít nhất hai host/project cần dùng chung hoặc đó là contract cross-process.
- Không đưa bounded-context business rule vào Shared.
- Shared web middleware không được reference worker-only concern.
- Shared contracts không reference Domain/Application/Infrastructure.

---

## 17. Naming Conventions

| Artifact | Convention hiện tại | Example |
| --- | --- | --- |
| Aggregate root | `<Name>` | `Order` |
| Entity | `<Name>` | `OrderLine` |
| Value object | `<Name>` | `Money`, `CustomerSnapshot` |
| Domain event | `<Action>DomainEvent` | `OrderCreatedDomainEvent` |
| Integration event | Past-tense business fact/request | `StockReserved`, `OrderConfirmationRequested` |
| MediatR request | Verb/use-case name | `CreateOrder`, `SearchOrders` |
| Handler | `<RequestName>Handler` | `CreateOrderHandler` |
| Validator | `<RequestName>Validator` | `CreateOrderValidator` |
| Repository interface | `I<Name>Repository` | `IOrderRepository` |
| Repository class | `<Name>Repository` | `OrderRepository` |
| DbContext | `<Service>DbContext` | `SalesDbContext` |
| EF configuration | `<Entity>Configuration` | `OrderConfiguration` |
| Specification | `<Rule>Specification` | `OrderCreatedFromSpecification` |
| Read service port | `I<Name>ReadService` | `IOrderReadService` |
| Read service impl | `<Name>ReadService` | `OrderReadService` |
| Hosted publisher | `<Service>OutboxPublisher` | `SalesOutboxPublisher` |

Namespace convention hiện tại khá flat ở một số project, ví dụ nhiều Sales.Application type dùng `namespace Sales.Application;`. Khi thêm file mới, ưu tiên match namespace pattern của folder đang sửa.

---

## 18. DTO / Model Rules

Rules:

- API request model dùng cho transport input.
- DTO/read model dùng cho Application output.
- Integration event contract dùng cho cross-service messaging.
- Không trả entity/aggregate ra ngoài API.
- Không dùng tuple cho business data.
- Required collection nên dùng `IReadOnlyCollection<T>` trong request/DTO khi không cần mutate.
- Optional value dùng nullable rõ ràng.
- Money/price đang theo VND, làm tròn zero decimal với `AwayFromZero` theo behavior hiện có.
- Phone value normalized về digits; search phone hỗ trợ prefix/suffix theo read service hiện có.

---

## 19. Async / Await

Rules:

- I/O operation phải async.
- Không dùng `.Result` hoặc `.Wait()`.
- Method async nên có hậu tố `Async`, trừ MediatR `Handle` theo interface.
- Luôn truyền `CancellationToken` ở Application/Infrastructure.
- Không dùng `ConfigureAwait(false)` trong ASP.NET Core nếu không có lý do rõ.

---

## 20. Dependency Injection

Composition root/pattern hiện tại:

```text
Sales.Api/Program.cs
Sales.Api/Extensions/*
Sales.Application/DependencyInjection.cs
Sales.Infrastructure/DependencyInjection.cs

Inventory.Api/Program.cs
Inventory.Api/Extensions/*
Inventory.Infrastructure/DependencyInjection.cs

AuditLog.Worker/Program.cs
AuditLog.Worker/DependencyInjection.cs
AuditLog.Infrastructure/DependencyInjection.cs
```

Rules:

- Application register validators và MediatR pipeline behaviors.
- Infrastructure register DbContext, repositories, read services, cache, Kafka, hosted publishers.
- Api/Worker gọi extension registration và cấu hình pipeline/host lifecycle.
- DbContext, UnitOfWork, repository, read services dùng scoped lifetime.
- Kafka publisher adapter có thể singleton nếu implementation thread-safe và không phụ thuộc scoped service.
- Không resolve service bằng service locator trong business code.

---

## 21. Docker Compose / Local Runtime

Compose file chính:

```text
docker/docker-compose.yml
```

Dịch vụ local chính:

```text
postgres
mongodb
redis
kafka
seq
elasticsearch
kibana
otel-collector
apm-server
sales-api
inventory-api
auditlog-worker
```

Rules:

- Không hard-code connection string trong code; dùng appsettings/env/compose.
- Container/service name rõ theo role.
- Healthcheck cho infrastructure critical khi thêm service mới.
- Migration chạy có kiểm soát; không tự ý drop database.
- Default local endpoints và cách verify nằm trong `README.md`.

---

## 22. Logging / Observability

Rules:

- Log structured bằng Serilog.
- Shared sink policy dùng `BuildingBlocks.Observability/SerilogBootstrap.cs`.
- HTTP request observability dùng `BuildingBlocks.Web` middleware/extensions.
- Không log password/token/raw sensitive payload.
- Exception phải được log ở boundary phù hợp.
- Correlation/trace context truyền qua HTTP header và Kafka message headers.
- Outbox/Inbox/Kafka consumer log message id/event id/correlation id khi xử lý lỗi hoặc retry.
- OpenTelemetry source/metrics đặt trong `Observability` folder của service/infrastructure tương ứng.

---

## 23. Testing Rules

Test projects hiện tại:

```text
Sales.Domain.Tests
Sales.Application.Tests
Sales.Infrastructure.Tests
Sales.Architecture.Tests
Inventory.Tests
Inventory.Infrastructure.Tests
AuditLog.Tests
tests/Playwright
```

Rules:

- Domain tests kiểm tra invariant và state transitions.
- Application tests kiểm tra command/query orchestration với mocked/fake ports.
- Infrastructure tests kiểm tra reliability, outbox/inbox, concurrency, persistence behavior.
- Architecture tests kiểm tra dependency rule và ngăn EF/Kafka leakage vào Domain/Application.
- AuditLog tests kiểm tra Mongo idempotency và mapping/storage behavior.
- Playwright tests kiểm tra API flow end-to-end khi services chạy.
- Reliability/integration tests chạm real Postgres/Mongo là opt-in qua `RUN_RELIABILITY_TESTS=true`.
- Test name mô tả behavior, ví dụ `ConfirmOrder_ShouldRejectStaleVersion_WhenVersionDoesNotMatch`.

Verify cơ bản:

```bash
dotnet test Sales.sln
docker compose -f docker/docker-compose.yml config
```

---

## 24. Angular Test Client

Frontend phụ trợ nằm ở:

```text
src/Web/Sales.TestClient/
```

Rules:

- Client này dùng để test thủ công API, không phải production frontend.
- Khi sửa contract/API response ảnh hưởng UI, cập nhật `api.service.ts`, `models.ts` và component tương ứng.
- Giữ proxy config trỏ đúng local API.
- Không đưa business rule vào frontend; frontend chỉ validate UX cơ bản và hiển thị lỗi API.

---

## 25. Common Anti-Patterns To Avoid

| Anti-pattern | Correct approach |
| --- | --- |
| Business logic trong Controller | Đưa vào Application handler hoặc Domain |
| Domain reference EF/Kafka/Redis/MediatR | Technical concern chỉ nằm Infrastructure/host |
| Application gọi DbContext trực tiếp trong command | Dùng repository/read port/unit of work |
| Handler publish Kafka trực tiếp | Ghi outbox trong transaction |
| Consumer xử lý không idempotent | Dùng inbox/unique event id |
| Repository gọi SaveChanges | Save qua UnitOfWork |
| Trả entity ra API | Trả DTO/response |
| Dùng tuple cho business data | Tạo DTO/value object/model rõ nghĩa |
| Query read-only có tracking | Dùng `AsNoTracking()` |
| Không truyền CancellationToken | Truyền xuyên suốt Application/Infrastructure |
| Raw SQL concat string | Dùng EF LINQ hoặc parameterized SQL |
| Hard-code topic/group | Dùng `KafkaTopics`/`KafkaConsumerGroups` |
| AuditLog update/delete tùy tiện | Audit nên append-only/idempotent |
| Bỏ qua ETag/concurrency khi sửa order | Dùng version/If-Match theo flow hiện có |

---

## 26. Final Rule

Khi viết code mới:

1. Xác định bounded context và layer.
2. Kiểm tra dependency rule.
3. Nếu là business invariant, đặt trong Domain.
4. Nếu là use case/orchestration, đặt trong Application command/query/handler.
5. Nếu là technical implementation, đặt trong Infrastructure.
6. Nếu là HTTP transport concern, đặt trong Api controller/model/middleware/extension.
7. Nếu là cross-service contract, đặt trong `BuildingBlocks.Contracts`.
8. Nếu phát sinh integration event, dùng Outbox.
9. Nếu consume integration event, dùng Inbox/idempotency.
10. Nếu query đọc dữ liệu, dùng read-service port và EF projection trong Infrastructure.
11. Nếu thêm dependency/shared code, kiểm tra lại architecture tests.
