# Audit logging

Tài liệu này mô tả hệ thống audit log hiện tại sau refactor hybrid: audit dữ liệu thường được tạo tự động từ EF Core `ChangeTracker`, còn audit có ý nghĩa nghiệp vụ đặc biệt được bổ sung bằng enricher hoặc event thủ công.

## Mục tiêu

- Entity CRUD mới được audit mặc định khi EF Core track thay đổi.
- Không cần tạo mapper riêng kiểu `ProductCreatedAuditMapper`, `CustomerUpdatedAuditMapper`.
- Audit event được ghi vào transactional Outbox cùng transaction với dữ liệu nghiệp vụ.
- Kafka chỉ publish từ Outbox publisher, không publish trực tiếp trong `SaveChanges`.
- AuditLog Worker chỉ lưu audit contract vào MongoDB, không phụ thuộc Sales.Domain hoặc Inventory.Domain.
- Dữ liệu nhạy cảm được ignore hoặc mask tập trung.

## Luồng tổng quát

```text
Command/API/Kafka Consumer
  -> Domain/Application thay đổi aggregate/entity
  -> EF Core ChangeTracker phát hiện Added/Modified/Deleted
  -> EfCoreAuditEntryFactory tạo AuditLogEvent
  -> IAuditEnricher bổ sung mô tả/metadata nếu cần
  -> AuditSaveChangesInterceptor add OutboxMessage
  -> Business data + audit outbox commit cùng transaction
  -> OutboxPublisherService publish Kafka
  -> AuditLog.Worker consume sales.audit.v1 / inventory.audit.v1
  -> MongoAuditWriter upsert MongoDB theo AuditId
```

## Contract dùng chung

Contract nằm trong `src/Shared/BuildingBlocks.Contracts/Auditing/`:

- `AuditLogEvent`
- `AuditChange`
- `AuditActions`

`AuditLogEvent` là payload chuẩn của audit topics. Các field chính:

- `AuditId`: idempotency key của audit document.
- `ServiceName`: service tạo audit (`Sales`, `Inventory`).
- `EntityType`, `EntityId`, `Action`.
- `ActorId`, `ActorName`, `CorrelationId`, `CausationId`, `TraceId`.
- `Changes`: danh sách `PropertyPath`, `OldValue`, `NewValue`.
- `Metadata`: metadata nghiệp vụ tùy chọn.
- `SchemaVersion`: hiện là `1`.

Contract cũ `AuditChanged` đã được xóa để tránh song song hai schema audit.

## Shared infrastructure

Các thành phần dùng chung nằm trong `src/Shared/BuildingBlocks.Infrastructure/Auditing/`:

- `AuditOptions`: cấu hình service name, topic, ignore/mask/truncate.
- `IAuditContextAccessor`: lấy actor/correlation/trace mà không phụ thuộc HTTP.
- `EfCoreAuditEntryFactory`: đọc `DbContext.ChangeTracker.Entries()`.
- `IAuditAggregateResolver`: gom child entity vào aggregate root.
- `IAuditEnricher`: thêm business meaning cho audit event.
- `AuditSaveChangesInterceptor`: add `OutboxMessage` trước commit.
- `AuditingServiceCollectionExtensions.AddAuditing(...)`.

Factory chỉ đọc EF scalar properties, không serialize object graph hoặc navigation property.

## Include, ignore, mask

`AuditOptions` mặc định bỏ qua dữ liệu nhạy cảm và technical fields:

- Ignore: password, token, secret, API key, connection string, payload.
- Mask: phone, email.
- Technical fields bị bỏ qua: `Version`, `CreatedAt`, `UpdatedAt`, `DeletedAt`, `ReversedPhone`.
- Binary value được lưu là `[binary]`.
- String dài bị truncate theo `MaximumStringLength`.
- Outbox/Inbox/Audit entities bị ignore để tránh vòng lặp.

Service có thể cấu hình thêm:

```csharp
services.AddAuditing(options =>
{
    options.ServiceName = "Sales";
    options.TopicName = KafkaTopics.SalesAudit;
    options.IgnoreEntity<OutboxMessage>();
    options.IgnoreEntity<InboxMessage>();
    options.IgnoreEntity<ApplicationUser>();
    options.IgnoreEntity<RefreshToken>();
});
```

## Sales

Registration nằm ở `Sales.Infrastructure/DependencyInjection.cs`.

Sales dùng:

- `SalesAuditContextAccessor`: adapter từ `IExecutionContext`.
- `SalesAuditAggregateResolver`: gom `OrderLine` vào audit event của `Order`, path dạng `Lines[ProductId=...].Quantity`.
- `OrderAuditEnricher`: thêm description cho các đổi trạng thái order.

`DomainEventMapper` hiện chỉ map integration event nghiệp vụ:

- `OrderConfirmationRequestedDomainEvent` -> `OrderConfirmationRequested`
- `OrderUndoComfirmedDomainEvent` -> `OrderCancellationRequested`

CRUD audit Product/Customer/Order không còn nằm trong `DomainEventMapper`.

## Inventory

Registration nằm ở `Inventory.Infrastructure/DependencyInjection.cs`.

Inventory dùng:

- `InventoryAuditAggregateResolver`: gom `ReservationLine` vào audit event của `Reservation`.
- `ReservationAuditEnricher`: thêm description cho status reservation.

`InventoryItem` được ignore khỏi auto audit để tránh duplicate với audit nghiệp vụ thủ công trong `InventoryEventOutbox.EnqueueInventoryAdjusted(...)`. Stock adjustment vẫn phát `AuditLogEvent` explicit vì cần actor và delta nghiệp vụ.

## AuditLog Worker

Worker chỉ consume audit topics:

- `sales.audit.v1`
- `inventory.audit.v1`

`MongoAuditWriter` deserialize `AuditLogEvent`, validate `SchemaVersion == 1`, normalize `JsonElement` values rồi upsert `events` theo `AuditId`.

Mongo indexes:

- unique `AuditId`
- `EntityType + EntityId + OccurredAt`
- `ServiceName + OccurredAt`
- `CorrelationId`

Worker không còn lưu mọi integration event raw như audit document chính. Integration events nghiệp vụ vẫn đi qua Kafka/Inbox như trước, nhưng audit trail chính là `AuditLogEvent`.

## Thêm audit cho entity CRUD mới

Thông thường không cần viết mapper:

1. Đảm bảo entity được EF Core track trong DbContext đã đăng ký `AuditSaveChangesInterceptor`.
2. Nếu entity chứa dữ liệu nhạy cảm, cấu hình `IgnoreProperty` hoặc `MaskProperty`.
3. Nếu entity con thuộc aggregate root, thêm mapping trong `IAuditAggregateResolver` của service.
4. Nếu cần mô tả nghiệp vụ, thêm `IAuditEnricher`.

Không tạo mapper CRUD riêng nếu chỉ làm diff old/new hoặc gán entity name/id.

## Thêm audit nghiệp vụ đặc biệt

Dùng `IAuditEnricher` khi dữ liệu diff đã có nhưng cần mô tả business reason.

Dùng explicit `AuditLogEvent` qua Outbox khi hành động không thể suy ra từ EF diff hoặc cần payload nghiệp vụ riêng, ví dụ:

- manual stock adjustment,
- login thất bại,
- role assignment,
- hành động admin không đổi aggregate domain trực tiếp.

## Kiểm tra

Backend tests:

```bash
dotnet test tests/Sales.Infrastructure.Tests/Sales.Infrastructure.Tests.csproj
dotnet test tests/AuditLog.Tests/AuditLog.Tests.csproj
```

End-to-end audit check bằng Playwright:

```bash
docker compose -f docker/docker-compose.yml build sales-api audit-worker
docker compose -f docker/docker-compose.yml up -d sales-api audit-worker

cd tests/Playwright
npm run test:audit
```

Playwright spec tạo/update product, sau đó dùng `tests/Playwright/AuditProbe` query MongoDB `audit.events` để xác minh document `ProductUpdated`.
