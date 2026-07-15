# Ghi chú review

Tài liệu này tổng hợp kết quả review code theo danh sách yêu cầu bài thực hành.

## Kết luận ngắn

Dự án đã đáp ứng phần lớn yêu cầu:

- Product catalog và search tên sản phẩm.
- Customer và search theo tên, phone đầu số, phone đuôi số.
- Order có thông tin khách hàng, tổng số lượng, tổng tiền, line có discount/quantity/price.
- Search order theo ngày tạo và thông tin khách hàng.
- Giải quyết hai người cùng sửa order bằng optimistic concurrency, ETag và `If-Match`.
- AuditLog dùng Kafka và MongoDB.
- Inventory là service riêng, DB riêng, có Outbox/Inbox.
- Dùng CQRS/MediatR trong Sales và Inventory.
- Dùng Repository, Unit of Work, Mapster, Redis cache, Redis distributed lock, Hangfire, Kafka.
- Docker Compose có Postgres, Redis, Mongo, Kafka, Seq, Elasticsearch, Kibana, APM Server, OpenTelemetry Collector.

## Finding quan trọng đã xử lý

### Inventory từng có thể sai tồn kho khi event đến lệch thứ tự

Mức độ ban đầu: High.

File liên quan:

- `src/Services/Inventory/Inventory.Infrastructure/Kafka/InventoryOrderEventProcessor.cs` (class cũ, đã xóa)
- `src/Services/Inventory/Inventory.Application/Commands/ReserveStock/ReserveStockCommandHandler.cs`
- `src/Services/Inventory/Inventory.Domain/Aggregates/Reservation.cs`

Tình huống đã được review:

1. Order đã được confirm và Inventory đang có reservation active.
2. User undo confirm, Sales publish release event.
3. User confirm lại, Sales publish confirmation event version mới.
4. Kafka có thể giao confirmation mới đến Inventory trước release cũ.
5. Code hiện tại gặp active reservation và version mới thì chỉ `AcknowledgeActive(...)`, enqueue `StockReserved`, rồi return.
6. Code không release stock cũ, không reserve theo line mới, không update reservation lines.
7. Release cũ đến sau sẽ bị bỏ qua vì stale version.

Vì sao nguy hiểm:

- Hệ thống có thể báo reserve thành công nhưng tồn kho/reservation lines không phản ánh order mới.
- Mục tiêu "Inventory không miss event" chưa đạt trọn vẹn về mặt đúng dữ liệu khi event lệch thứ tự.

Đã xử lý:

- Thêm `Reservation.ReplaceActive(...)` để cập nhật reservation lines khi confirmation version mới hơn đến lúc reservation vẫn active.
- `ReserveStockCommandHandler` tính delta giữa line cũ và line mới, release stock cũ và reserve stock mới trong cùng transaction.
- Chỉ enqueue `StockReserved` sau khi stock và reservation đã được cập nhật.
- Thêm test `ReserveStockHandlerTests.Newer_confirmation_replaces_active_reservation_lines_and_stock`.

## Điểm cần lưu ý khác

### Kafka topic partition

Trạng thái hiện tại:

- Code có topic constants và consumer group constants.
- Docker có `kafka-init` tạo topic từ `KafkaTopics.cs` với 3 partitions và replication factor 1.
- Producer dùng `AggregateId` làm message key để giữ ordering theo aggregate trong một partition.
- Code log partition/offset khi consume/publish.

Cần lưu ý:

- Kafka không đảm bảo global ordering trên toàn topic; ordering chỉ nằm trong từng partition.
- Consumer group quyết định cách các consumer instance chia nhau partition.

### Reliability tests và CI

Trạng thái hiện tại:

- Reliability tests dùng database thật đã gắn `[Trait("Category", "Reliability")]` và gate bằng `RUN_RELIABILITY_TESTS=true`; khi không set thì no-op, `dotnet test` mặc định vẫn xanh.
- Có test tự động cho: Outbox retry → publish một lần, Outbox max-retry → dead-letter, Outbox replay, Inbox idempotency, stale event, audit idempotency, optimistic concurrency.
- CI `.github/workflows/ci.yml` tách `fast-checks` (mọi push/PR, chạy `Category!=Reliability`) và `reliability-tests` (push `main`/`workflow_dispatch`, có Postgres + Mongo service container, upload trx/log khi fail).
- Chi tiết: [reliability-tests.md](reliability-tests.md).

Cần lưu ý:

- Hai kịch bản cần live Kafka (consumer lỗi trước commit offset, process restart) hiện là thủ tục manual, chưa tự động hóa để giữ CI đơn giản/ổn định.
- Không tuyên bố exactly-once; tài liệu dùng ngôn ngữ at-least-once + idempotent consumer.

### Observability dashboard

Trạng thái hiện tại:

- Code export logs/traces/metrics.
- Docker có Seq, Elasticsearch, Kibana, APM Server, OpenTelemetry Collector.
- Hướng dẫn demo nằm ở [monitoring-demo.md](monitoring-demo.md).

Cần lưu ý:

- Đã có dashboard export `docker/kibana/exports/sales-management-reliability.ndjson` + import script `docker/kibana/import-dashboards.sh` + service `kibana-init` tự import khi `docker compose up`.
- NDJSON soạn thủ công theo schema Kibana 9.1, **cần xác nhận lại bằng một lần import thật** trên stack đang chạy.
- Screenshot chưa chụp được (không chạy Docker/Kibana trong môi trường hiện tại); vị trí + checklist ở `docs/images/monitoring/README.md`.

### Inventory đã refactor sang MediatR/CQRS

Trạng thái hiện tại:

- Sales dùng CQRS/MediatR đầy đủ.
- Inventory API gọi `ISender.Send(...)`.
- Inventory Kafka adapter map integration event sang `ReserveStockCommand` hoặc `ReleaseStockCommand`.
- Inventory use case hiện hữu đã có command/query handlers trong `Inventory.Application`.

Cần lưu ý:

- Không tạo thêm command/query giả định nếu chưa có use case thật.
- Chi tiết audit nằm ở [inventory-cqrs-refactor-audit.md](inventory-cqrs-refactor-audit.md).

### README cũ có link docs không tồn tại

Trạng thái:

- README cũ có một số link docs như `docs/project-presentation.md`, `docs/kafka-usage-guide.md`, `docs/Redis-cache-usage-guide.md` nhưng trong repo hiện tại không có các file đó.

Đã xử lý:

- README hiện trỏ về `docs/tech/README.md`.
- Bộ tài liệu chính nằm trong `docs/tech/`.

## Lệnh đã verify

Đã chạy:

```bash
dotnet build Sales.sln --no-restore --verbosity:minimal
dotnet test Sales.sln --no-build --no-restore --verbosity:minimal
docker compose -f docker/docker-compose.yml config
```

Kết quả:

- Build pass, 0 warnings, 0 errors.
- Pass 78 tests.
- Docker Compose config hợp lệ.

Ghi chú:

- Lần chạy trong sandbox fail do MSBuild không tạo được IPC socket.
- Chạy ngoài sandbox thì pass.
- Docker daemon không xác minh được trong môi trường hiện tại vì user không có quyền Docker socket; `sudo` cần terminal/password.
