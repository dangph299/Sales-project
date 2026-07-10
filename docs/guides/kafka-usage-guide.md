# Kafka usage guide trong Sales Management

Tài liệu này giải thích riêng cách project đang dùng Kafka: Kafka nằm ở đâu, khởi tạo thế nào, producer/consumer được gọi ra sao trong từng project, vì sao có Outbox/Inbox, và nếu phát triển tiếp thì nên tái sử dụng/mở rộng như thế nào. Phần distributed tracing xuyên Kafka (`ActivitySource`, W3C `traceparent`) nằm ở [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 5, không lặp lại ở đây.

## Tóm tắt nhanh

- Kafka (KafkaFlow) là kênh giao tiếp bất đồng bộ duy nhất giữa Sales, Inventory, AuditLog — không service nào gọi HTTP trực tiếp sang service khác cho nghiệp vụ chính (mục 1).
- Publish/consume đi qua **Outbox** (Sales, Inventory) và **Inbox** (cả 3 service) để đảm bảo at-least-once không mất event và không xử lý trùng (mục 15–16).
- Partition key = `AggregateId` (`orderId` cho Order event) để giữ ordering trong cùng 1 order (mục 7).
- Muốn thêm event Kafka mới, làm theo 6 bước ở mục 18. Muốn debug Kafka bị kẹt/mất event, xem case đã fix ở cuối mục 17 và lệnh kiểm tra ở mục 21.

## 1. Kafka dùng để làm gì trong project?

Kafka được dùng cho giao tiếp bất đồng bộ giữa các process:

```text
Sales.Api
  -> Kafka
  -> Inventory.Api
  -> Kafka
  -> Sales.Api

Sales.Api / Inventory.Api
  -> Kafka
  -> AuditLog.Worker
```

Mục tiêu:

- Tách Sales và Inventory thành 2 process/database riêng.
- Khi confirm order, Sales không gọi trực tiếp Inventory bằng HTTP.
- Inventory xử lý reserve tồn kho async.
- AuditLog nhận event từ nhiều service và lưu MongoDB.
- Delivery theo kiểu at-least-once, chống mất event bằng Outbox/Inbox.

## 2. Package Kafka đang dùng

Project dùng Kafka thông qua KafkaFlow.

Các package chính:

```text
KafkaFlow
KafkaFlow.Microsoft.DependencyInjection
KafkaFlow.Serializer.JsonCore
KafkaFlow.LogHandler.Microsoft
```

KafkaFlow giúp:

- Đăng ký producer/consumer trong DI.
- Serialize/deserialize message JSON.
- Tạo Kafka bus và start/stop theo lifecycle app.
- Map message vào typed handler `IMessageHandler<T>`.

## 3. Kafka broker nằm ở đâu?

Kafka chạy bằng Docker Compose:

```text
docker/docker-compose.yml
```

Service:

```yaml
kafka:
  image: apache/kafka:4.1.1
  ports: ["9094:9094"]
  environment:
    KAFKA_PROCESS_ROLES: broker,controller
    KAFKA_LISTENERS: CONTROLLER://:9093,PLAINTEXT://:9092,EXTERNAL://:9094
    KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:9092,EXTERNAL://localhost:9094
    KAFKA_AUTO_CREATE_TOPICS_ENABLE: "true"
```

Ý nghĩa:

- Trong Docker network, app dùng broker `kafka:9092`.
- Từ máy host, có thể dùng `localhost:9094`.
- Kafka chạy KRaft mode, không cần ZooKeeper.
- Local MVP bật auto-create topic để giảm setup thủ công.

Chạy:

```bash
sudo docker compose -f docker/docker-compose.yml up -d --build
```

## 4. Event contract nằm ở đâu?

Shared event contracts nằm ở:

```text
src/Shared/BuildingBlocks.Contracts/
```

Envelope chung:

```csharp
public sealed record EventEnvelope(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    long Version,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset OccurredAt,
    string Actor,
    JsonElement Data)
```

Ý nghĩa:

| Field | Ý nghĩa |
|---|---|
| `EventId` | ID duy nhất của event, dùng cho Inbox/Mongo dedup |
| `EventType` | Loại event, ví dụ `OrderConfirmationRequested` |
| `AggregateId` | ID aggregate chính, với order event là `orderId` |
| `Version` | Version aggregate khi event được tạo |
| `CorrelationId` | Gom các event cùng một workflow |
| `CausationId` | Event trước đó gây ra event hiện tại |
| `OccurredAt` | Thời điểm UTC |
| `Actor` | User/system tạo event |
| `Data` | Payload JSON |

Các payload hiện có:

```csharp
public sealed record OrderConfirmationRequested(Guid OrderId, IReadOnlyCollection<OrderLineIntegration> Lines);
public sealed record OrderCancellationRequested(Guid OrderId);
public sealed record StockReserved(Guid OrderId);
public sealed record StockRejected(Guid OrderId, string Reason);
public sealed record StockReleased(Guid OrderId);
public sealed record AuditChanged(string Entity, string Action, string? Before, string? After);
```

## 5. Danh sách topic hiện tại

| Topic | Producer | Consumer | Mục đích |
|---|---|---|---|
| `sales.audit.v1` | Sales | AuditLog | Audit Product/Customer/Order |
| `inventory.audit.v1` | Inventory | AuditLog | Audit điều chỉnh tồn kho |
| `sales.order-confirmation-requested.v1` | Sales | Inventory, AuditLog | Sales yêu cầu reserve tồn |
| `sales.order-undo-confirmation-requested.v1` | Sales | Inventory, AuditLog | Sales yêu cầu release tồn |
| `inventory.stock-reserved.v1` | Inventory | Sales, AuditLog | Inventory báo reserve thành công |
| `inventory.stock-rejected.v1` | Inventory | Sales, AuditLog | Inventory báo thiếu hàng |
| `inventory.stock-released.v1` | Inventory | Sales, AuditLog | Inventory báo release xong |

Quy ước đặt tên:

```text
<bounded-context>.<business-event>.v<version>
```

Ví dụ:

```text
sales.order-confirmation-requested.v1
inventory.stock-reserved.v1
```

Nếu thay đổi breaking contract, tạo topic `.v2` thay vì sửa payload `.v1`.

## 6. Consumer group hiện tại

| Consumer group | Project | Subscribe |
|---|---|---|
| `inventory-orders-v1` | Inventory.Api | `sales.order-confirmation-requested.v1`, `sales.order-undo-confirmation-requested.v1` |
| `sales-inventory-results-v1` | Sales.Api | `inventory.stock-reserved.v1`, `inventory.stock-rejected.v1`, `inventory.stock-released.v1` |
| `audit-mongodb-v1` | AuditLog.Worker | Toàn bộ topic audit/integration |

Giải thích:

- Mỗi consumer group nhận một bản copy riêng của event.
- Inventory và AuditLog khác group nên cùng nhận được order confirmation event.
- Nếu scale nhiều instance cùng group, Kafka chia partition giữa các instance.

## 7. Partition key đang dùng

Producer publish bằng:

```csharp
await producer.ProduceAsync(row.Topic, envelope.AggregateId.ToString(), envelope);
```

Key là:

```text
AggregateId
```

Với Order event:

```text
AggregateId = orderId
```

Lợi ích:

- Event của cùng order đi vào cùng partition.
- Kafka giữ ordering trong cùng partition.
- Tránh event cùng order bị xử lý đảo thứ tự.

Nếu sau này event không thuộc Order:

- Customer event: key nên là `customerId`.
- Product event: key nên là `productId`.
- Payment event: key nên là `paymentId` hoặc `orderId` nếu cần ordering theo order.

## 8. Kafka khởi tạo trong Sales.Api như thế nào?

Đăng ký Kafka nằm ở:

```text
src/Services/Sales/Sales.Infrastructure/DependencyInjection.cs
```

Code chính:

```csharp
services.AddKafka(kafka => kafka
    .AddCluster(cluster => cluster
        .WithBrokers(brokers)
        .AddProducer("sales-outbox", producer =>
            producer.AddMiddlewares(x => x.AddSerializer<JsonCoreSerializer>()))
        .AddConsumer(consumer => consumer
            .Topics([
                "inventory.stock-reserved.v1",
                "inventory.stock-rejected.v1",
                "inventory.stock-released.v1"])
            .WithGroupId("sales-inventory-results-v1")
            .WithAutoOffsetReset(AutoOffsetReset.Earliest)
            .WithBufferSize(100)
            .WithWorkersCount(4)
            .AddMiddlewares(x => x
                .AddDeserializer<JsonCoreDeserializer>()
                .AddTypedHandlers(h => h.AddHandler<SalesIntegrationEventHandler>())))));
```

Sales có:

- Producer name: `sales-outbox`
- Consumer handler: `SalesIntegrationEventHandler`
- Consumer group: `sales-inventory-results-v1`

Bus được start trong:

```text
src/Services/Sales/Sales.Api/Program.cs
```

```csharp
var kafkaBus = app.Services.CreateKafkaBus();
await kafkaBus.StartAsync();
app.Lifetime.ApplicationStopping.Register(() => kafkaBus.StopAsync().GetAwaiter().GetResult());
```

## 9. Sales publish event như thế nào?

Sales không publish Kafka trực tiếp trong controller hoặc command handler.

Flow đúng:

```text
Controller
  -> MediatR command
  -> Aggregate raise domain event
  -> SalesDbContext.SaveChangesAsync
  -> DomainEventMapper
  -> insert outbox_messages
  -> SalesOutboxPublisher publish Kafka
```

Ví dụ confirm order:

```text
POST /api/orders/{id}/confirm
  -> ConfirmOrder command
  -> Order.RequestConfirmation()
  -> raise OrderConfirmationRequestedDomainEvent
  -> SaveChangesAsync()
  -> insert outbox row topic sales.order-confirmation-requested.v1
```

Map Domain Event sang Kafka Integration Event nằm ở:

```text
src/Services/Sales/Sales.Infrastructure/Kafka/DomainEventMapper.cs
```

Ví dụ:

```csharp
OrderConfirmationRequestedDomainEvent e =>
    ("sales.order-confirmation-requested.v1",
        new OrderConfirmationRequested(
            e.OrderId,
            e.Lines.Select(x => new OrderLineIntegration(x.ProductId, x.Sku, x.Quantity)).ToArray()))
```

Sau đó `SalesOutboxPublisher` (background service polling mỗi 2 giây, mục 15) đọc outbox và gọi `IOutboxPublisher.PublishAsync`, implementation thật (`KafkaOutboxPublisher.cs`) mới thực sự produce:

```csharp
var producer = producers.GetProducer("sales-outbox");
await producer.ProduceAsync(row.Topic, envelope.AggregateId.ToString(), envelope, headers);
```

`KafkaOutboxPublisher` còn mở 1 `Activity` (tracing) và ghi header W3C `traceparent`/`tracestate` trước khi produce — đây là phần distributed tracing xuyên Kafka, xem chi tiết ở [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 5 thay vì lặp lại ở đây.

## 10. Sales consume event như thế nào?

Sales consume kết quả từ Inventory:

```text
inventory.stock-reserved.v1
inventory.stock-rejected.v1
inventory.stock-released.v1
```

Handler:

```text
src/Services/Sales/Sales.Infrastructure/Kafka/SalesIntegrationEventHandler.cs
```

Class:

```csharp
public sealed class SalesIntegrationEventHandler : IMessageHandler<EventEnvelope>
```

Logic:

```text
1. Insert EventId vào inbox_messages trong transaction
2. Nếu duplicate EventId -> rollback và return success
3. Load Order theo AggregateId
4. Switch EventType:
   - StockReserved -> order.MarkReserved()
   - StockRejected -> order.RejectInventory(reason)
   - StockReleased -> hiện chưa đổi trạng thái thêm
5. SaveChanges
6. Commit transaction
```

Vì sao insert Inbox trước?

- Kafka là at-least-once, message có thể đến lại.
- `EventId` unique giúp consumer xử lý idempotent.
- Duplicate thì bỏ qua, không update order lần hai.

## 11. Kafka khởi tạo trong Inventory.Api như thế nào?

Đăng ký nằm ở:

```text
src/Services/Inventory/Inventory.Infrastructure/DependencyInjection.cs
```

Inventory có:

- Producer name: `inventory-outbox`
- Consumer group: `inventory-orders-v1`
- Handler: `InventoryEventHandler`

Code chính:

```csharp
services.AddKafka(kafka => kafka.AddCluster(cluster => cluster.WithBrokers(brokers)
    .AddProducer("inventory-outbox", p =>
        p.AddMiddlewares(x => x.AddSerializer<JsonCoreSerializer>()))
    .AddConsumer(c => c
        .Topics([
            "sales.order-confirmation-requested.v1",
            "sales.order-undo-confirmation-requested.v1"])
        .WithGroupId("inventory-orders-v1")
        .WithAutoOffsetReset(AutoOffsetReset.Earliest)
        .WithBufferSize(100)
        .WithWorkersCount(4)
        .AddMiddlewares(x => x
            .AddDeserializer<JsonCoreDeserializer>()
            .AddTypedHandlers(h => h.AddHandler<InventoryEventHandler>())))));
```

Bus được start trong:

```text
src/Services/Inventory/Inventory.Api/Program.cs
```

```csharp
var bus = app.Services.CreateKafkaBus();
await bus.StartAsync();
app.Lifetime.ApplicationStopping.Register(() => bus.StopAsync().GetAwaiter().GetResult());
```

## 12. Inventory consume event như thế nào?

Handler:

```text
src/Services/Inventory/Inventory.Infrastructure/Kafka/InventoryEventHandler.cs
```

Inventory nhận:

```text
sales.order-confirmation-requested.v1
sales.order-undo-confirmation-requested.v1
```

Logic confirmation:

```text
1. Begin transaction Serializable
2. Insert EventId vào Inbox
3. Nếu duplicate -> rollback và return success
4. Deserialize OrderConfirmationRequested
5. Load InventoryItem theo ProductId
6. Nếu thiếu hàng:
     enqueue inventory.stock-rejected.v1 vào Inventory Outbox
7. Nếu đủ hàng:
     item.Reserve(quantity)
     tạo Reservation
     enqueue inventory.stock-reserved.v1 vào Inventory Outbox
8. SaveChanges
9. Commit
```

Logic cancellation:

```text
1. Load Reservation theo orderId
2. Nếu không có hoặc đã Released -> return
3. Release từng InventoryItem
4. reservation.Release()
5. enqueue inventory.stock-released.v1 vào Outbox
```

## 13. Inventory publish event như thế nào?

Inventory không publish trực tiếp trong handler. Nó enqueue vào Outbox:

```csharp
db.Enqueue(
    EventEnvelope.Create(
        request.OrderId,
        envelope.Version,
        new StockReserved(request.OrderId),
        "inventory",
        envelope.CorrelationId,
        envelope.EventId),
    "inventory.stock-reserved.v1");
```

Outbox publish nằm ở:

```text
src/Services/Inventory/Inventory.Infrastructure/Kafka/InventoryOutboxPublisher.cs
```

Producer:

```csharp
var producer = producers.GetProducer("inventory-outbox");
await producer.ProduceAsync(row.Topic, envelope.AggregateId.ToString(), envelope);
```

## 14. Kafka trong AuditLog.Worker

AuditLog không publish event, chỉ consume.

Khởi tạo nằm ở:

```text
src/Services/AuditLog/AuditLog.Worker/Program.cs
```

Consumer group:

```text
audit-mongodb-v1
```

Subscribe:

```text
sales.audit.v1
inventory.audit.v1
sales.order-confirmation-requested.v1
sales.order-undo-confirmation-requested.v1
inventory.stock-reserved.v1
inventory.stock-rejected.v1
inventory.stock-released.v1
```

Handler:

```text
src/Services/AuditLog/AuditLog.Infrastructure/Mongo/AuditEventHandler.cs
```

Logic:

```text
1. Nhận EventEnvelope
2. Tạo unique index EventId trong Mongo collection events
3. Upsert document theo EventId
4. Lưu cả topic, partition, offset
```

Bus lifecycle nằm ở:

```text
src/Services/AuditLog/AuditLog.Worker/Hosting/KafkaBusService.cs
```

```csharp
_bus = services.CreateKafkaBus();
await _bus.StartAsync(cancellationToken);
```

## 15. Outbox hoạt động chi tiết

Outbox là bảng trong database của từng service.

Sales:

```text
SalesDbContext.OutboxMessages -> table outbox_messages
```

Inventory:

```text
InventoryDbContext.Outbox -> table outbox_messages
```

Cột quan trọng:

| Cột | Ý nghĩa |
|---|---|
| `Id` | Trùng với `EventId` |
| `Topic` | Topic cần publish |
| `Payload` | Serialized `EventEnvelope` |
| `OccurredAt` | Thời điểm event |
| `ProcessedAt` | Đã publish thành công hay chưa |
| `Attempts` | Số lần thử publish |
| `NextAttemptAt` | Lúc được retry tiếp |
| `DeadLetteredAt` | Đã vào DLQ hay chưa |
| `LockedUntil` | Lock timeout khi một publisher claim message |
| `LockId` | ID batch claim |
| `LastError` | Lỗi publish gần nhất |

Publisher loop:

```text
Mỗi 2 giây:
  1. Tìm row ProcessedAt null
  2. DeadLetteredAt null
  3. NextAttemptAt null hoặc <= now
  4. LockId/LockedUntil hết hạn
  5. Claim bằng LockId
  6. Publish Kafka
  7. Thành công -> ProcessedAt = now
  8. Thất bại -> Attempts++, NextAttemptAt = now + backoff
  9. Attempts >= MaxAttempts -> DeadLetteredAt = now
```

Backoff hiện tại:

```text
2^attempts seconds, tối đa 300 giây
```

Max attempts:

```text
10
```

## 16. Inbox hoạt động chi tiết

Inbox là bảng dedup consumer.

Sales:

```text
inbox_messages(EventId, ProcessedAt, Consumer)
```

Inventory:

```text
inbox_messages(EventId, ProcessedAt)
```

Quy tắc:

- `EventId` là primary key.
- Consumer insert `EventId` trước khi xử lý.
- Nếu insert bị unique violation, nghĩa là event đã xử lý.
- Duplicate được coi là success để Kafka không retry vô ích.

## 17. Các flow Kafka quan trọng

### Flow confirm order đủ hàng

```text
1. User gọi POST /api/orders/{id}/confirm
2. Sales Order chuyển Draft -> PendingInventory
3. SalesDbContext insert outbox topic sales.order-confirmation-requested.v1
4. SalesOutboxPublisher publish Kafka
5. InventoryEventHandler nhận event
6. Inventory insert Inbox
7. Inventory reserve stock và tạo Reservation
8. Inventory enqueue outbox topic inventory.stock-reserved.v1
9. InventoryOutboxPublisher publish Kafka
10. SalesIntegrationEventHandler nhận event
11. Sales insert Inbox
12. Sales Order chuyển PendingInventory -> Confirmed
13. AuditLog.Worker lưu event vào MongoDB
```

### Flow confirm order thiếu hàng

```text
1. Sales phát sales.order-confirmation-requested.v1
2. Inventory nhận event
3. Inventory thấy thiếu available stock
4. Inventory enqueue inventory.stock-rejected.v1
5. Sales nhận StockRejected
6. Sales Order chuyển PendingInventory -> InventoryRejected
7. Order lưu RejectionReason
```

### Flow cancel order đã confirmed

```text
1. User gọi POST /api/orders/{id}/cancel
2. Sales Order chuyển Confirmed -> Cancelled
3. Sales enqueue sales.order-undo-confirmation-requested.v1
4. Inventory release Reservation
5. Inventory enqueue inventory.stock-released.v1
6. Sales nhận StockReleased; hiện tại không đổi thêm trạng thái vì Order đã Cancelled
```

### Flow Kafka down

```text
1. Sales/Inventory commit DB thành công
2. Outbox row đã tồn tại
3. Kafka publish fail
4. Publisher tăng Attempts, set NextAttemptAt
5. Kafka phục hồi
6. Publisher retry và publish lại
7. Consumer dùng Inbox chống duplicate
```

### Bug đã fix: order kẹt PendingInventory sau cold start (consumer group mới)

Triệu chứng: confirm order thỉnh thoảng không bao giờ chuyển từ `PendingInventory`
sang `Confirmed`/`InventoryRejected` — không có exception nào trong Seq, Outbox
`ProcessedAt` vẫn được set (Sales publish thành công), nhưng phía nhận
(`inventory-orders-v1` hoặc `sales-inventory-results-v1`) không bao giờ xử lý event đó.

Root cause: `AddConsumer(...)` của Sales và Inventory không set `AutoOffsetReset`,
nên Confluent.Kafka dùng default `largest` (= `latest`). Kết hợp với
`KAFKA_AUTO_CREATE_TOPICS_ENABLE: true` (không có bước pre-create topic), mỗi khi
consumer group chưa có committed offset (lần đầu stack chạy, hoặc sau khi xoá Kafka
data volume) sẽ có race: nếu producer publish event đầu tiên (tự động tạo topic)
trước khi consumer group hoàn tất join/assign partition, consumer bắt đầu đọc từ
`latest` — tức là **sau** message đó — và bỏ lỡ event vĩnh viễn, không log lỗi gì.
`AuditLog.Worker` không bị ảnh hưởng vì nó đã set `WithAutoOffsetReset(AutoOffsetReset.Earliest)` từ đầu.

Bằng chứng verify (2026-07-08): chạy `tests/Playwright/specs/kafka-flow.spec.ts` lặp
lại ngay sau khi stack cold start — 2 run đầu tiên timeout ở bước poll `Confirmed`;
kiểm tra `GET /api/orders` cho thấy đúng 2 order đó (confirm lúc `01:14:45Z` và
`01:15:18Z`, ngay sau cụm log `Subscribed topic not available` lúc `01:10:25Z`) vẫn
kẹt `PendingInventory` vĩnh viễn, trong khi mọi order confirm sau đó (từ `01:15:50Z`
trở đi, khi consumer group đã có committed offset) đều `Confirmed` trong vài giây.

Fix: thêm `.WithAutoOffsetReset(AutoOffsetReset.Earliest)` vào consumer của Sales
(`sales-inventory-results-v1`) và Inventory (`inventory-orders-v1`), giống pattern
đã có sẵn ở `AuditLog.Worker`. `auto.offset.reset` chỉ có tác dụng khi consumer group
chưa có committed offset hợp lệ, nên fix này an toàn — không đổi hành vi của group đã
chạy ổn định.

Lưu ý khi verify lại: vì `inventory-orders-v1` và `sales-inventory-results-v1` hiện
đã có committed offset (do đã consume thành công từ `01:15:50Z`), restart
`sales-api`/`inventory-api` bình thường **sẽ không** re-trigger race này. Muốn thấy
fix hoạt động rõ ràng cần một consumer group hoàn toàn mới (ví dụ
`docker compose down -v` để xoá Kafka data volume rồi `up` lại, hoặc đổi tạm
`Kafka:AuditGroupId`-style override sang group id mới) rồi confirm order ngay lần đầu.

## 18. Cách thêm event Kafka mới

Ví dụ muốn thêm event `OrderPaid`.

### Bước 1: Thêm contract

Trong:

```text
src/Shared/BuildingBlocks.Contracts/IntegrationEvents/
```

Thêm:

```csharp
public sealed record OrderPaid(Guid OrderId, decimal PaidAmount);
```

### Bước 2: Thêm domain event nếu event xuất phát từ Sales aggregate

Trong Sales Domain:

```text
src/Services/Sales/Sales.Domain/Events/DomainEvents.cs
```

Ví dụ:

```csharp
public sealed record OrderPaidDomainEvent(Guid OrderId, decimal PaidAmount) : IDomainEvent;
```

Aggregate raise event khi nghiệp vụ xảy ra.

### Bước 3: Map domain event sang topic

Trong:

```text
src/Services/Sales/Sales.Infrastructure/Kafka/DomainEventMapper.cs
```

Thêm case:

```csharp
OrderPaidDomainEvent e =>
    ("sales.order-paid.v1", new OrderPaid(e.OrderId, e.PaidAmount))
```

### Bước 4: Consumer subscribe topic

Nếu Inventory hoặc service khác cần nghe:

```csharp
.Topics(["sales.order-paid.v1"])
```

hoặc thêm vào danh sách topic hiện có.

### Bước 5: Handler xử lý EventType

Trong handler:

```csharp
switch (envelope.EventType)
{
    case nameof(OrderPaid):
        var data = envelope.Data.Deserialize<OrderPaid>()!;
        // xử lý
        break;
}
```

### Bước 6: AuditLog subscribe nếu cần audit

Thêm topic vào AuditLog Worker:

```text
src/Services/AuditLog/AuditLog.Worker/Program.cs
```

## 19. Cách tái sử dụng khi phát triển tiếp

### Nên tái sử dụng

- `EventEnvelope` cho mọi integration event.
- Topic naming convention `.v1`.
- Partition key = aggregate id.
- Transactional Outbox khi publish event từ service có database.
- Inbox dedup cho mọi consumer có side effect.
- CorrelationId/CausationId để trace workflow.
- Retry/backoff/DLQ fields trong Outbox.
- Custom metrics outbox backlog/deadletters.

### Nên tách thành shared library sau này

Hiện Sales và Inventory có code OutboxPublisher khá giống nhau. Khi phát triển tiếp, nên tách:

```text
BuildingBlocks.Messaging
BuildingBlocks.Outbox
BuildingBlocks.Inbox
BuildingBlocks.Observability
```

Có thể tạo abstraction:

```csharp
public interface IOutboxMessage
{
    Guid Id { get; }
    string Topic { get; }
    string Payload { get; }
    DateTimeOffset? ProcessedAt { get; set; }
    DateTimeOffset? NextAttemptAt { get; set; }
    DateTimeOffset? DeadLetteredAt { get; set; }
    int Attempts { get; set; }
}
```

Và generic publisher:

```csharp
OutboxPublisher<TDbContext, TOutboxMessage>
```

### Nên cải thiện trong production

Local MVP đang ổn cho bài thực hành, nhưng production nên thêm:

- Tạo topic bằng IaC/script thay vì auto-create.
- Cấu hình partition count/replication factor rõ ràng.
- Schema registry hoặc contract test cho event versioning.
- DLQ topic riêng, ví dụ `sales.order-confirmation-requested.v1.dlq`.
- Dashboard Kafka consumer lag.
- Alert khi outbox backlog/deadletters tăng.
- Move Mongo index creation ra startup, không tạo trong từng message.
- Idempotency có consumer name nếu một service có nhiều handler cho cùng event.
- Transaction isolation và lock strategy được benchmark với tải cao.

## 20. Các vị trí code cần nhớ

| Mục | File |
|---|---|
| Event contracts | `src/Shared/BuildingBlocks.Contracts/IntegrationEvents/` |
| Sales Kafka registration | `src/Services/Sales/Sales.Infrastructure/DependencyInjection.cs` |
| Sales bus startup | `src/Services/Sales/Sales.Api/Program.cs` |
| Sales outbox publish loop | `src/Services/Sales/Sales.Infrastructure/Kafka/SalesOutboxPublisher.cs` |
| Sales Kafka produce + tracing | `src/Services/Sales/Sales.Infrastructure/Kafka/KafkaOutboxPublisher.cs` |
| Sales integration consumer | `src/Services/Sales/Sales.Infrastructure/Kafka/SalesIntegrationEventHandler.cs` |
| Sales domain event mapper | `src/Services/Sales/Sales.Infrastructure/Kafka/DomainEventMapper.cs` |
| Sales outbox/inbox tables | `src/Services/Sales/Sales.Infrastructure/Persistence/SalesDbContext.cs` |
| Inventory Kafka registration | `src/Services/Inventory/Inventory.Infrastructure/DependencyInjection.cs` |
| Inventory bus startup | `src/Services/Inventory/Inventory.Api/Program.cs` |
| Inventory consumer | `src/Services/Inventory/Inventory.Infrastructure/Kafka/InventoryEventHandler.cs` |
| Inventory outbox publisher | `src/Services/Inventory/Inventory.Infrastructure/Kafka/InventoryOutboxPublisher.cs` |
| Inventory outbox/inbox tables | `src/Services/Inventory/Inventory.Infrastructure/Persistence/InventoryDbContext.cs` |
| Audit Kafka registration | `src/Services/AuditLog/AuditLog.Worker/Program.cs` |
| Audit bus lifecycle | `src/Services/AuditLog/AuditLog.Worker/Hosting/KafkaBusService.cs` |
| Audit consumer | `src/Services/AuditLog/AuditLog.Infrastructure/Mongo/AuditEventHandler.cs` |
| Kafka Docker | `docker/docker-compose.yml` |

## 21. Lệnh kiểm tra nhanh Kafka local

Start stack:

```bash
sudo docker compose -f docker/docker-compose.yml up -d --build
```

Xem container:

```bash
sudo docker compose -f docker/docker-compose.yml ps
```

Xem logs Kafka:

```bash
sudo docker compose -f docker/docker-compose.yml logs -f kafka
```

Xem logs app publish/consume:

```bash
sudo docker compose -f docker/docker-compose.yml logs -f sales-api inventory-api audit-worker
```

Chạy smoke test:

```bash
cd tests/Playwright
npm run test:smoke
```

Nếu Kafka tạm down rồi up lại, kiểm tra:

- `outbox_messages.Attempts`
- `outbox_messages.NextAttemptAt`
- `outbox_messages.ProcessedAt`
- `outbox_messages.DeadLetteredAt`
- Seq logs với event id/order id
- Kibana metrics `sales.outbox.backlog`, `inventory.outbox.backlog`
