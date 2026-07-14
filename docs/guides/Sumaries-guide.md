# Sumaries guide — Tổng hợp bài thực hành Sales Management DDD

Tài liệu này là **điểm vào duy nhất**: chốt lại yêu cầu bài thực hành đã đáp ứng đến đâu, pattern nào đã dùng ở đâu, tài liệu chi tiết nào nên đọc tiếp, và **những gap thật sự còn tồn tại** sau khi rà soát code (không phải suy đoán). Đọc file này trước, rồi đi sâu vào từng guide theo nhu cầu.

## 1. Yêu cầu bài thực hành — đã đáp ứng đến đâu?

| Yêu cầu | Trạng thái | Vị trí chính |
|---|---|---|
| Danh mục sản phẩm, search tên sản phẩm, soft delete | ✅ Đủ | `ProductsController`, `Create/Update/DeleteProduct`, `SearchProducts`, GIN trigram index trên `Name`, query filter `!IsDelete` |
| Khách hàng, search phone đầu/đuôi + tên, soft delete | ✅ Đủ | `Customer.cs` (normalize phone), `ReversedPhone` cho search suffix, `CustomerReadService`, query filter `!IsDelete` |
| Audit columns | ✅ Đủ | `UpdatedAt` trên Product/Customer/Order; `IsDelete`/`DeleteByUser`/`DeletedAt` trên Product/Customer |
| Đơn hàng: info khách hàng, tổng SL, tổng tiền, chi tiết (chiết khấu, SL, giá) | ✅ Đủ | Aggregate `Order`/`OrderLine`, VO `Money` (`AwayFromZero` rounding), `discountPercent` bắt buộc để tránh silent default `0` |
| Search order theo ngày tạo / tên / SĐT khách hàng | ✅ Đủ | `OrderReadService`, index ngày/tên/SĐT trong `SalesDbContext` |
| Giải quyết 2 người cùng sửa đơn hàng | ✅ Đủ | Optimistic concurrency: `Order.Version` + `ETag`/`If-Match` + 409 Conflict — xem `project-presentation.md` §7, §18 |
| AuditLog lưu MongoDB, nhận qua Kafka | ✅ Đủ | `AuditLog.Worker` consume Kafka, ghi Mongo unique `EventId`, **giờ log vào Seq** (đã fix, xem mục 4) |
| Inventory service riêng, bảng riêng, không miss event | ✅ Đủ | `src/Services/Inventory` độc lập, Postgres riêng, Outbox/Inbox transactional, `Reservation.LastOrderVersion` chống event cũ đến trễ |
| CQRS (command/query) qua MediatR | ✅ Đủ (Sales + Inventory) | `Sales.Application/Commands`, `Queries`; `Inventory.Application/Commands`, `Queries`, `InventoryTransactionBehavior`; HTTP/Kafka adapters dispatch qua `ISender` |
| Factory Method | ✅ Đủ | `Product.Create`, `Customer.Create`, `Order.Create`, `ProductSnapshot.Create`, `CustomerSnapshot.Create`, `Money.Vnd` — 6 static factory, đều có private constructor giữ invariant |
| Repository | ✅ Đủ | `IRepository<T>`/`Repository<T>` + `IProductRepository`/`IOrderRepository` cho query đặc thù |
| Unit of Work | ✅ Đủ | `SalesDbContext` implement `IUnitOfWork` (`Shared/BuildingBlocks.Application/Persistence/IUnitOfWork.cs`, dùng chung — không còn khai báo riêng trong `Sales.Application`), tách `UnitOfWork.cs` mỏng |
| AutoMapper/Mapster | ✅ Đủ (đã sửa hiểu nhầm cũ) | Xem mục 2 — Mapster **có dùng thật** cho Product/Customer; Order mapping tay có lý do rõ ràng |
| Redis cache (cache-aside + distributed lock) | ✅ Đủ | Cache-aside `ProductDto` (`ProductCache`), lock SET-NX/Lua cho Hangfire cleanup job — xem [Redis-cache-usage-guide.md](Redis-cache-usage-guide.md) |
| Hangfire (queue) | ✅ Đủ | 3 queue (`critical`/`default`/`maintenance`), job cleanup + replay outbox/DLQ, dashboard `/hangfire` |
| Kafka (topic, group, partition) | ✅ Đủ, tài liệu sâu sẵn có | 7 topic `.v1`, 3 consumer group, partition key = `AggregateId` — xem [kafka-usage-guide.md](kafka-usage-guide.md) |
| Database: Postgres, MongoDB | ✅ Đủ | Postgres cho Sales/Inventory/Hangfire, MongoDB cho AuditLog |
| Docker / docker-compose | ✅ Đủ | `docker/docker-compose.yml` — đủ toàn bộ service kể trên |
| Monitoring: Seq, APM Elastic (Kibana), OpenTelemetry (trace/metric) | ✅ Đủ | Toàn bộ 5 gap quan sát được ở đợt audit trước đã được vá trong code — xem mục 4 |

**Kết luận**: yêu cầu chức năng nghiệp vụ (Product/Customer/Order/concurrency/AuditLog/Inventory) và toàn bộ pattern bắt buộc đều đã có, đúng vị trí, đúng lý do kiến trúc. Phần "Monitoring" cũng đã được hoàn thiện — AuditLog log vào Seq, Kafka consumer log structured, trace nối xuyên Kafka qua W3C `traceparent`, EF Core query có span riêng, và `LogContext` giờ có tác dụng thật cho cả HTTP request lẫn Kafka consumer.

## 2. Đính chính: Mapster có dùng, không phải "unused"

`ARCHITECTURE_CHECKLIST.md` (dòng ghi chú Sales.Application) từng ghi: *"Mapster được reference trong .csproj nhưng KHÔNG được dùng — mapping thực tế là extension method tay."* Sau khi rà lại code, ghi chú này **sai** và đã được sửa lại trong file đó. Thực tế:

`src/Services/Sales/Sales.Application/DTOs/DtoMapping.cs`:

```csharp
using Mapster;

private static readonly TypeAdapterConfig Config = CreateConfig();

public static ProductDto ToDto(this Product product) => product.Adapt<ProductDto>(Config);
public static CustomerDto ToDto(this Customer customer) => customer.Adapt<CustomerDto>(Config);
public static OrderDto ToDto(this Order order) => new(order.Id, order.CustomerId, ...); // tay, không qua Mapster

private static TypeAdapterConfig CreateConfig()
{
    var config = new TypeAdapterConfig();
    config.NewConfig<Product, ProductDto>().Map(x => x.Price, x => x.Price.Amount);
    config.NewConfig<Customer, CustomerDto>();
    return config;
}
```

- `ProductDto`/`CustomerDto`: map thật qua `.Adapt<T>()` với `TypeAdapterConfig` tùy chỉnh (projection `Price.Amount`).
- `OrderDto`: map tay bằng constructor, vì `Order` có collection lồng (`Lines`) và nhiều `Money.Amount` cần unwrap — map tay ở đây rõ ràng hơn generic adapter.

Đây là inconsistency nhỏ (2/3 DTO qua Mapster, 1/3 map tay) chứ không phải "pattern chưa dùng". Không cần coi là gap phải fix gấp.

## 3. Bản đồ tài liệu

| Tài liệu | Nội dung |
|---|---|
| [architecture.md](architecture.md) | Dependency rule, vai trò từng folder theo bounded context |
| [project-presentation.md](project-presentation.md) | Trình bày tổng thể bài thực hành, mapping yêu cầu ↔ implementation, demo flow |
| [kafka-usage-guide.md](kafka-usage-guide.md) | Kafka: KafkaFlow, topic/group/partition, Outbox/Inbox, cách thêm event mới |
| [kafka-playwright-debug-guide.md](kafka-playwright-debug-guide.md) | Quy trình debug flow Kafka Sales↔Inventory bằng Playwright + kiểm tra tay (curl/Seq) khi order kẹt/chậm rời `PendingInventory` |
| [Redis-cache-usage-guide.md](Redis-cache-usage-guide.md) | Redis: cache-aside Product, distributed lock Hangfire job |
| [Seqlog-usage-guide.md](Seqlog-usage-guide.md) | Serilog/Seq: cấu hình từng service, enricher, gap log AuditLog |
| [Elastic-usage-guide.md](Elastic-usage-guide.md) | OpenTelemetry → OTel Collector → APM Server → Elasticsearch → Kibana, custom metric, giới hạn tracing qua Kafka |
| [observability.md](observability.md) | Contract dashboard Kibana cần dựng (panel, ngưỡng cảnh báo, replay) |
| `ARCHITECTURE_CHECKLIST.md` | Checklist hiện trạng kiến trúc theo từng project, dùng khi review PR |
| `CODING_RULES.md` | Quy tắc bắt buộc: 1 type/file, dependency direction, vị trí file theo vai trò |

## 4. Gap quan sát được ở đợt audit trước — đã fix trong code

Đợt audit trước liệt kê 5 gap quan sát được (không phải suy đoán) ở phần Monitoring. Cả 5 đã được vá trực tiếp trong code (không chỉ ghi nhận trong docs):

1. **`AuditLog.Worker` giờ log vào Seq.** Thêm `Serilog.Extensions.Hosting`, `Serilog.Settings.Configuration`, `Serilog.Sinks.Console`, `Serilog.Sinks.Seq` vào `AuditLog.Worker.csproj`; `Program.cs` gọi `builder.Services.AddSerilog((_, cfg) => cfg.ReadFrom.Configuration(...).WriteTo.Console().WriteTo.Seq(...))`; `appsettings.json` có `Seq:Url`/`Serilog:MinimumLevel`. Chi tiết: [Seqlog-usage-guide.md](Seqlog-usage-guide.md) mục 4.3.
2. **Kafka consumer handler giờ log structured theo từng event.** `SalesIntegrationEventHandler`, `InventoryEventHandler`, `AuditEventHandler` đều inject `ILogger<T>` và log `EventType`/`EventId`/`CorrelationId`/`Topic`/`GroupId` ở 3 mốc: handling, handled (success), failed (exception) — không log `envelope.Data` (payload). Chi tiết: [Seqlog-usage-guide.md](Seqlog-usage-guide.md) mục 6.
3. **Distributed tracing giờ xuyên qua Kafka.** `KafkaOutboxPublisher` (Sales và Inventory) mở `Activity` kiểu `Producer` và ghi `traceparent`/`tracestate` (W3C) vào Kafka header qua `BuildingBlocks.Contracts.MessageHeaders.TraceParent`/`.TraceState` (2 hằng số mới, tái sử dụng class có sẵn nhưng trước đây chết code) và `KafkaFlow.IMessageHeaders`. Consumer (`SalesIntegrationEventHandler`, `InventoryEventHandler`, `AuditEventHandler`) đọc lại header, parse bằng `ActivityContext.TryParse`, và mở `Activity` con đúng parent — verify thực nghiệm cho thấy `TraceId` khớp và `ParentSpanId` đúng span cha. Chi tiết: [Elastic-usage-guide.md](Elastic-usage-guide.md) mục 8.
4. **EF Core instrumentation đã thêm.** `OpenTelemetry.Instrumentation.EntityFrameworkCore` (1.16.0-beta.1) + `.AddEntityFrameworkCoreInstrumentation()` trong `Sales.Api`/`Inventory.Api` — query Postgres giờ xuất hiện thành span con trong trace. Chi tiết: [Elastic-usage-guide.md](Elastic-usage-guide.md) mục 2.
5. **`LogContext` giờ có tác dụng thật.** `CorrelationLoggingMiddleware` (mới, `Sales.Api`/`Inventory.Api`, đặt sau `UseAuthorization()`) push `Service`/`CorrelationId`/`RequestPath`/`UserId` bao quanh toàn bộ MediatR command/query handler execution của mỗi HTTP request; cả 3 Kafka consumer handler push `Service`/`EventId`/`EventType`/`CorrelationId` bao quanh việc xử lý từng event. Chi tiết: [Seqlog-usage-guide.md](Seqlog-usage-guide.md) mục 6.

`Inventory.Api` thiếu `Serilog:MinimumLevel` trong `appsettings.json` vẫn còn (inconsistency nhỏ, không ảnh hưởng chức năng, không nằm trong 5 gap chính được yêu cầu fix).

## 5. Thay đổi nghiệp vụ mới đã phản ánh trong code

1. **Soft delete Product/Customer.** API mới `DELETE /api/products/{id}` và `DELETE /api/customers/{id}` chỉ set `IsDelete = true`, `DeleteByUser`, `DeletedAt`, cập nhật `UpdatedAt`/`Version`; read/search mặc định ẩn record đã xóa bằng EF query filter. Product cache được invalidate khi delete. Angular test client có nút `Delete` cho cả Product và Customer.
2. **Audit timestamp.** `AggregateRoot.Touch()` (`Shared/BuildingBlocks.Domain/Abstractions/AggregateRoot.cs`, dùng chung với Inventory từ 2026-07-10) cập nhật `UpdatedAt`, vì vậy Product/Customer/Order đều trả `updatedAt` trong DTO. Migration Sales mới: `SoftDeleteAndUpdatedAt`.
3. **Discount input không được thiếu.** `OrderLineInput.DiscountPercent` là nullable và validator bắt buộc `NotNull().InclusiveBetween(0, 100)`; nếu client gửi nhầm key như `discount` thì bị reject thay vì lưu ngầm `0`.
4. **Reconfirm/undo confirm bền hơn trước event đến trễ.** Inventory `Reservation` lưu `LastOrderVersion`; release event version cũ bị bỏ qua, confirm mới hơn trên active reservation vẫn publish `StockReserved` để Sales không chờ mãi. Migration Inventory mới: `ReservationOrderVersion`.
5. **Playwright regression specs.** `reconfirm-flow.spec.ts` kiểm tra `create -> confirm -> undo confirm -> confirm`; `over-available-after-undo.spec.ts` kiểm tra `create -> confirm -> undo confirm -> update quantity > available -> confirm` và chờ kết quả `InventoryRejected`.

## 6. Lệnh chạy nhanh tổng hợp

```bash
# Build + test
dotnet restore Sales.sln --disable-parallel
dotnet build Sales.sln --no-restore
dotnet test Sales.sln --no-build --no-restore

# Toàn bộ hạ tầng local
sudo docker compose -f docker/docker-compose.yml up -d --build
sudo docker compose -f docker/docker-compose.yml ps

sudo docker compose -f docker/docker-compose.yml stop kibana apm-server elasticsearch otel-collector
sudo docker compose -f docker/docker-compose.yml up kibana apm-server elasticsearch otel-collector -d --build

# Reliability test cần Postgres/Mongo thật
RUN_RELIABILITY_TESTS=true dotnet test Sales.sln

# Angular test client
cd src/Web/Sales.TestClient && npm install && npm start

# Playwright smoke test
cd tests/Playwright && npm install && npm run test:smoke
```

Endpoint tổng hợp:

```text
Sales API:      http://localhost:5000
Inventory API:  http://localhost:5001
Angular FE:     http://localhost:4200
Seq:            http://localhost:8081
Kibana:         http://localhost:5601
Hangfire:       http://localhost:5000/hangfire
Redis:          localhost:6379
Kafka:          localhost:9094
```
