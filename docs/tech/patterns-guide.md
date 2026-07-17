# Hướng dẫn pattern cho người mới

Tài liệu này giải thích các pattern đang dùng trong repo theo hướng thực tế: pattern là gì, vì sao dùng, code nằm ở đâu, khi thêm code mới phải làm thế nào và lỗi nào nên tránh.

## 1. DDD: Domain-Driven Design

DDD là cách thiết kế phần mềm xoay quanh nghiệp vụ. Thay vì bắt đầu từ database table, ta bắt đầu từ các khái niệm nghiệp vụ như `Order`, `Product`, `Customer`, `InventoryItem`.

Trong repo này:

- Sales quản lý sản phẩm, khách hàng, đơn hàng.
- Inventory quản lý tồn kho và reservation.
- AuditLog ghi lại sự kiện.

Code nằm ở:

- `src/Services/Sales/Sales.Domain/`
- `src/Services/Inventory/Inventory.Domain/`
- `src/Shared/BuildingBlocks.Domain/`

Quy tắc:

- Business rule đặt trong Domain.
- Domain không biết database, HTTP, Kafka, Redis.
- Aggregate root là nơi bảo vệ invariant của một cụm dữ liệu.
- Bên ngoài không nên sửa entity con trực tiếp nếu entity đó thuộc aggregate.

Ví dụ:

- `Order` là aggregate root.
- `OrderLine` thuộc `Order`.
- Muốn thay line của đơn hàng thì gọi `Order.ReplaceLines(...)`, không sửa trực tiếp list line.

## 2. Aggregate Root

Aggregate root là object chính đại diện cho một ranh giới nhất quán. Mọi thay đổi bên trong aggregate nên đi qua aggregate root.

Code nằm ở:

- Base class: `src/Shared/BuildingBlocks.Domain/Abstractions/AggregateRoot.cs`
- `Order`: `src/Services/Sales/Sales.Domain/Aggregates/Order.cs`
- `Product`: `src/Services/Sales/Sales.Domain/Aggregates/Product.cs`
- `Customer`: `src/Services/Sales/Sales.Domain/Aggregates/Customer.cs`
- `Reservation`: `src/Services/Inventory/Inventory.Domain/Aggregates/Reservation.cs`

Base class có:

- `Version`: dùng cho optimistic concurrency.
- `UpdatedAt`: thời điểm cập nhật.
- Domain events: các sự kiện được raise sau khi aggregate thay đổi.

Quy tắc:

- Method của aggregate nên đặt tên theo nghiệp vụ: `RequestConfirmation`, `ReplaceLines`, `MarkReserved`.
- Không expose collection mutable ra ngoài.
- Khi state thay đổi, aggregate nên tăng version nếu cần.
- Nếu có event nghiệp vụ, aggregate raise domain event; Infrastructure sẽ map sang integration event.

## 3. Value Object

Value object là object không có identity riêng, so sánh bằng giá trị.

Code nằm ở:

- `src/Services/Sales/Sales.Domain/ValueObjects/`
- Ví dụ: `Money`, `CustomerSnapshot`, `ProductSnapshot`, `OrderLineItem`

Quy tắc:

- Dùng value object khi một nhóm field có ý nghĩa nghiệp vụ.
- Nên immutable.
- Không đặt EF/Kafka/HTTP logic trong value object.
- Không dùng tuple cho business data đi qua nhiều layer.

## 4. Domain Event và Integration Event

Domain event là sự kiện nội bộ domain. Integration event là sự kiện gửi qua service khác bằng Kafka.

Code nằm ở:

- Domain events Sales: `src/Services/Sales/Sales.Domain/Events/`
- Integration contracts: `src/Shared/BuildingBlocks.Contracts/IntegrationEvents/`
- Mapper domain event sang integration event: `src/Services/Sales/Sales.Infrastructure/Kafka/DomainEventMapper.cs`

Quy tắc:

- Domain event nằm trong Domain và không biết Kafka topic.
- Integration event nằm trong `BuildingBlocks.Contracts`.
- Mapper nằm trong Infrastructure vì liên quan transport/event envelope.
- Topic nằm trong `KafkaTopics`, không hardcode.
- CRUD audit thông thường không cần domain event riêng; hệ thống audit đọc EF Core `ChangeTracker` và ghi `AuditLogEvent` vào Outbox.

## 5. CQRS

CQRS tách lệnh ghi và lệnh đọc:

- Command: thay đổi dữ liệu.
- Query: chỉ đọc dữ liệu.

Trong repo này, Sales dùng CQRS với MediatR.

Code nằm ở:

- Command: `src/Services/Sales/Sales.Application/Features/<Aggregate>/Commands/`
- Query: `src/Services/Sales/Sales.Application/Features/<Aggregate>/Queries/`
- CQRS marker: `src/Shared/BuildingBlocks.Application/Abstractions/Messaging/`
- MediatR registration: `src/Services/Sales/Sales.Api/Extensions/ServiceCollectionExtensions.cs`

Quy tắc:

- Command record đặt trong `Features/<Aggregate>/Commands/`.
- Query record đặt trong `Features/<Aggregate>/Queries/`.
- Handler là class riêng, cùng folder với command/query.
- Command handler gọi repository/domain/unit of work.
- Query handler gọi read service.
- Controller chỉ gọi `ISender.Send(...)`.

## 6. MediatR pipeline behavior

MediatR behavior là middleware quanh handler. Nó có thể validate, log, đo performance trước/sau khi handler chạy.

Code nằm ở:

- `src/Shared/BuildingBlocks.Application/Behaviors/`
- Registration: `src/Shared/BuildingBlocks.Application/DependencyInjection.cs`

Behavior đang có:

- `ValidationBehavior`: chạy FluentValidation.
- `LoggingBehavior`: log request.
- `PerformanceBehavior`: log request chậm.
- `ErrorLoggingBehavior`: log lỗi có phân loại.

Quy tắc:

- Validation nên đặt trong validator, không lặp lại trong controller.
- Behavior là cross-cutting concern, không chứa business rule riêng của Sales.
- Behavior dùng chung mới đưa vào BuildingBlocks; behavior chỉ dùng một service thì để trong service đó.

## 7. Repository

Repository là cổng truy cập aggregate cho command-side. Nó giấu chi tiết EF Core khỏi Application.

Code nằm ở:

- Interface: `src/Services/Sales/Sales.Domain/Repositories/`
- Implementation: `src/Services/Sales/Sales.Infrastructure/Repositories/`

Quy tắc:

- Repository interface nằm trong Domain.
- Repository implementation nằm trong Infrastructure.
- Repository không expose `IQueryable`.
- Search/read model nên dùng read service, không biến repository thành query engine.
- Không đặt `SaveChanges` trong repository; commit thuộc Unit of Work.

## 8. Unit of Work

Unit of Work gom nhiều thay đổi thành một lần commit.

Code nằm ở:

- Interface: `src/Shared/BuildingBlocks.Application/Persistence/IUnitOfWork.cs`
- Sales implementation: `src/Services/Sales/Sales.Infrastructure/UnitOfWork/UnitOfWork.cs`
- Real commit/outbox logic: `src/Services/Sales/Sales.Infrastructure/Persistence/DbContexts/SalesDbContext.cs`

Quy tắc:

- Handler gọi domain behavior trước, sau đó `uow.SaveChangesAsync`.
- Không gọi `SaveChangesAsync` nhiều lần trong một use case nếu không có lý do rõ.
- Outbox message nên được tạo trong cùng transaction với state change.
- Audit outbox rows được tạo bởi `AuditSaveChangesInterceptor` trong cùng transaction; không publish Kafka trực tiếp trong handler/interceptor.

## 8.1 Audit logging hybrid

Audit logging trong repo dùng hai cơ chế:

- **Tự động**: `EfCoreAuditEntryFactory` đọc `ChangeTracker` để tạo `AuditLogEvent` cho Added/Modified/Deleted.
- **Bổ sung nghiệp vụ**: `IAuditEnricher` thêm `Description`, `EventType` hoặc metadata khi field diff chưa đủ nghĩa.
- **Explicit event**: dùng khi hành động không thể suy ra từ EF diff, ví dụ manual inventory adjustment.

Code nằm ở:

- Contract: `src/Shared/BuildingBlocks.Contracts/Auditing/`
- Shared implementation: `src/Shared/BuildingBlocks.Infrastructure/Auditing/`
- Sales resolver/enricher: `src/Services/Sales/Sales.Infrastructure/Auditing/`
- Inventory resolver/enricher: `src/Services/Inventory/Inventory.Infrastructure/Auditing/`
- Worker/Mongo: `src/Services/AuditLog/`

Quy tắc:

- Không tạo mapper CRUD riêng nếu chỉ lấy entity name/id hoặc so sánh old/new.
- Dữ liệu nhạy cảm phải ignore/mask bằng `AuditOptions`.
- Entity con nên được gom về aggregate root bằng `IAuditAggregateResolver`.
- Audit worker không reference Domain của Sales/Inventory.

## 9. Factory Method

Factory method là method tạo object có rule, thường tên `Create`.

Code nằm ở:

- `Product.Create`
- `Customer.Create`
- `Order.Create`
- `OrderLine.Create`
- `Reservation.Create`
- `EventEnvelopeFactory.Create`

Quy tắc:

- Dùng factory khi object cần validate invariant lúc tạo.
- Constructor có thể private/protected cho ORM.
- Factory domain không gọi database/Kafka.
- Factory Infrastructure có thể xử lý serialize/metadata transport.

## 10. Mapster mapping

Mapping chuyển domain object sang DTO.

Code nằm ở:

- Mapping register theo feature: `src/Services/Sales/Sales.Application/Features/<Aggregate>/Mapping/<Aggregate>MappingRegister.cs`
- Đăng ký mapping dùng chung: `src/Shared/BuildingBlocks.Application/Mapping/MappingRegistrationExtensions.cs`

Quy tắc:

- Mapping của một feature đặt trong `Mapping/` của chính feature đó.
- Mapping đơn giản dùng Mapster.
- Mapping phức tạp map tay.
- Không đặt rule nghiệp vụ mới trong mapping.
- DTO là shape trả về cho API/read side, không phải aggregate.

## 11. Redis cache-aside

Cache-aside là pattern:

1. Thử đọc cache.
2. Nếu cache miss, đọc database.
3. Ghi kết quả vào cache.

Code nằm ở:

- Redis registration: `src/Services/Sales/Sales.Infrastructure/DependencyInjection.cs`
- Cache base: `src/Services/Sales/Sales.Infrastructure/ExternalServices/CacheService.cs`
- Product cache: `src/Services/Sales/Sales.Infrastructure/ExternalServices/ProductCache.cs`
- Cache decorator: `src/Services/Sales/Sales.Infrastructure/Persistence/ReadServices/CachedProductReadService.cs`

Quy tắc:

- Cache không phải nguồn sự thật.
- Cache key phải có prefix rõ.
- Cache TTL phải có giới hạn.
- Khi command sửa/xóa data, cần remove/update cache liên quan.

## 12. Redis distributed lock

Distributed lock giúp nhiều instance không chạy cùng một job tại một thời điểm.

Code nằm ở:

- `src/Services/Sales/Sales.Infrastructure/Hangfire/Jobs/MaintenanceCleanupJob.cs`

Cách đang dùng:

- Job cleanup tạo key Redis bằng `StringSet` với `When.NotExists`.
- Nếu lock thành công thì cleanup.
- Cuối cùng xóa lock bằng Lua script để chỉ owner mới xóa được lock.

Inventory cleanup dùng lock theo transaction của Postgres thay vì Redis:

- `src/Services/Inventory/Inventory.Infrastructure/Maintenance/InventoryMaintenanceService.cs`
- `src/Services/Inventory/Inventory.Infrastructure/Maintenance/InventoryMaintenanceWorker.cs`
- Worker chạy định kỳ và dùng `pg_try_advisory_xact_lock` để chỉ một instance cleanup.

Quy tắc:

- Lock phải có TTL.
- Unlock phải check token.
- Lock không thay thế transaction database.

## 13. Hangfire

Hangfire dùng để chạy background job và recurring job.

Code nằm ở:

- Registration: `src/Services/Sales/Sales.Api/Extensions/ServiceCollectionExtensions.cs`
- Dashboard: `src/Services/Sales/Sales.Api/Extensions/ApplicationBuilderExtensions.cs`
- Job class: `src/Services/Sales/Sales.Infrastructure/Hangfire/Jobs/MaintenanceCleanupJob.cs`
- Register recurring job: `src/Services/Sales/Sales.Api/Extensions/StartupTaskExtensions.cs`

Mỗi recurring job tách thành ba phần, mỗi phần một file:

```text
Sales.Infrastructure/Hangfire/
├── Jobs/           # chỉ logic thực thi job, không biết cron/queue/job ID
├── Definitions/    # đăng ký job: job ID, queue, cron, job type, method, time zone
├── Options/        # SalesRecurringJobsOptions (root) + options riêng từng job
├── SalesRecurringJobIds.cs        # job ID cố định của Sales
└── SalesRecurringJobsExtensions.cs # DI cho Sales recurring jobs
```

Phần kỹ thuật dùng chung nằm ở `src/Shared/BuildingBlocks.Infrastructure/Hangfire/`:

- `IRecurringJobDefinition`: contract một method `Register()`.
- `RecurringJobDefinitionBase`: job `Enabled` thì `AddOrUpdate`, job disabled thì
  `RemoveIfExists` khỏi storage.
- `RecurringJobSettings`: `Enabled`/`Cron`/`Queue` + validate cron/queue. Không chứa
  `JobId` — ID là hằng số của từng service.
- `RecurringJobRegistrarExtensions`: `serviceProvider.RegisterRecurringJobs()` gọi tất cả
  definitions; không biết gì về Sales hay Inventory.

Inventory không dùng Hangfire cho cleanup. Inventory dùng hosted worker riêng để dọn
processed Inbox/Outbox bằng Postgres advisory lock.

Quy tắc:

- Job đặt trong Infrastructure nếu nó làm việc với database/cache.
- Job nên idempotent.
- Nếu job có thể chạy trên nhiều instance, cần lock hoặc cơ chế tránh duplicate.
- Job implementation không được biết cron, queue, job ID hay cách gọi `AddOrUpdate`.
- Options nghiệp vụ **compose** `RecurringJobSettings` (property `Schedule`), không kế thừa,
  để shared settings không bị mở rộng bởi thuộc tính nghiệp vụ.
- Thao tác recovery thủ công (replay/reset dead-letter) **không phải** recurring job — đặt ở
  `Sales.Infrastructure/Maintenance/SalesMaintenanceService.cs`, không đặt trong `Hangfire/`.
- Queue name phải rõ nghĩa, ví dụ `maintenance`.

## 14. Kafka

Kafka là message broker. Service publish event vào topic, service khác consume topic bằng consumer group.

Code nằm ở:

- Topic: `src/Shared/BuildingBlocks.Contracts/Messaging/KafkaTopics.cs`
- Group: `src/Shared/BuildingBlocks.Contracts/Messaging/KafkaConsumerGroups.cs`
- Sales Kafka DI: `src/Services/Sales/Sales.Infrastructure/DependencyInjection.cs`
- Inventory Kafka DI: `src/Services/Inventory/Inventory.Infrastructure/DependencyInjection.cs`
- Audit Kafka DI: `src/Services/AuditLog/AuditLog.Worker/DependencyInjection.cs`
- Shared publisher: `src/Shared/BuildingBlocks.Infrastructure/Kafka/KafkaOutboxPublisher.cs`

Quy tắc:

- Tên topic/group khai báo tập trung.
- Producer publish qua Outbox.
- Consumer xử lý idempotent nếu event có thể bị deliver lại.
- Handler throw nếu fail để message không bị xem là thành công.
- Log topic, group, partition, offset để debug.

## 15. Outbox pattern

Outbox pattern giải quyết lỗi: database commit thành công nhưng publish Kafka fail.

Cách làm:

1. Trong cùng transaction với business data, ghi thêm row vào outbox table.
2. Background publisher đọc outbox.
3. Publish Kafka.
4. Chỉ mark processed sau khi Kafka ack.

Code nằm ở:

- Shared outbox row: `src/Shared/BuildingBlocks.Infrastructure/Outbox/OutboxMessage.cs`
- Shared publisher service base: `src/Shared/BuildingBlocks.Infrastructure/Outbox/OutboxPublisherService.cs`
- Kafka publisher: `src/Shared/BuildingBlocks.Infrastructure/Kafka/KafkaOutboxPublisher.cs`
- Sales DbContext enqueue: `src/Services/Sales/Sales.Infrastructure/Persistence/DbContexts/SalesDbContext.cs`
- Sales outbox publisher: `src/Services/Sales/Sales.Infrastructure/Kafka/SalesOutboxPublisher.cs`
- Inventory outbox publisher: `src/Services/Inventory/Inventory.Infrastructure/Kafka/InventoryOutboxPublisher.cs`

Quy tắc:

- Không publish Kafka trực tiếp trong command handler.
- Outbox row phải có retry info.
- Publisher phải claim/lock row để nhiều instance không publish cùng row cùng lúc.
- Có dead-letter hoặc replay mechanism cho message fail nhiều lần.

## 16. Inbox pattern

Inbox pattern giúp consumer idempotent khi Kafka deliver lại message.

Cách làm:

1. Consumer nhận event.
2. Insert `EventId` vào inbox table.
3. Nếu unique violation, đó là duplicate, skip.
4. Nếu insert thành công, xử lý business change.
5. Commit inbox và business change cùng transaction.

Code nằm ở:

- Inbox entity dùng chung: `src/Shared/BuildingBlocks.Infrastructure/Inbox/InboxMessage.cs`
- EF mapping Sales: `src/Services/Sales/Sales.Infrastructure/Persistence/Configurations/InboxMessageConfiguration.cs`
- EF mapping Inventory: `src/Services/Inventory/Inventory.Infrastructure/Persistence/Configurations/InboxMessageConfiguration.cs`
- Inbox adapter Inventory: `src/Services/Inventory/Inventory.Infrastructure/Persistence/InventoryInbox.cs`
- Sales processor: `src/Services/Sales/Sales.Infrastructure/Kafka/SalesInventoryEventProcessor.cs`
- Inventory adapter: `src/Services/Inventory/Inventory.Infrastructure/Kafka/InventoryIntegrationEventProcessor.cs`
- Inventory command handlers: `src/Services/Inventory/Inventory.Application/Commands/ReserveStock/`, `src/Services/Inventory/Inventory.Application/Commands/ReleaseStock/`

Quy tắc:

- Insert inbox trước khi làm side effect.
- Unique index trên `EventId`.
- Duplicate phải skip an toàn.
- Nếu business change fail, transaction rollback cả inbox để message có thể retry.

## 17. Optimistic concurrency

Optimistic concurrency cho phép hai người đọc cùng một bản ghi, nhưng khi ghi thì phát hiện ai đang ghi trên version cũ.

Code nằm ở:

- Version field: `AggregateRoot`
- EF concurrency token: `OrderConfiguration`
- HTTP ETag: `ControllerEtagExtensions`
- Command expected version: order commands trong `Sales.Application/Features/Orders/Commands/`

Quy tắc:

- Endpoint update existing order phải yêu cầu `If-Match`.
- Command phải có `ExpectedVersion`.
- Handler phải check version trước khi sửa.
- API conflict trả 409.

## 18. OpenTelemetry, Seq, Elasticsearch, Kibana

Observability giúp debug hệ thống phân tán:

- Logs: xem sự kiện đang xảy ra.
- Traces: xem request/event đi qua service nào.
- Metrics: xem số lượng, latency, backlog, fail.

Code nằm ở:

- Serilog shared: `src/Shared/BuildingBlocks.Observability/SerilogBootstrap.cs`
- OpenTelemetry shared: `src/Shared/BuildingBlocks.Web/Observability/OpenTelemetryExtensions.cs`
- Metrics: `src/Shared/BuildingBlocks.Infrastructure/Observability/Metrics/`
- Docker stack: `docker/docker-compose.yml`

Quy tắc:

- Mỗi service nên có service name riêng.
- Kafka message nên propagate trace context.
- Log nên có correlation id.
- Dashboard là phần cấu hình vận hành, không nên hardcode trong business logic.
