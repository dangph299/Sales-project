# Reliability tests

Tài liệu này mô tả các đảm bảo (guarantee) về độ tin cậy của hệ thống, cách chúng được kiểm chứng bằng test, và cách chạy các test đó local cũng như trong CI.

## Guarantee hệ thống cung cấp

Hệ thống sử dụng **transactional Outbox**, **at-least-once delivery** qua Kafka, và **idempotent consumer** (Inbox) để tránh mất event đã commit và để xử lý an toàn khi một message bị giao lại.

- **Transactional Outbox** — Domain event được ghi vào bảng `outbox_messages` trong **cùng transaction** với thay đổi nghiệp vụ. Nếu transaction commit, event chắc chắn tồn tại để publish; nếu rollback, cả hai cùng biến mất. Không có cửa sổ "đã đổi state nhưng mất event".
- **At-least-once delivery** — Outbox publisher chỉ đánh dấu `ProcessedAt` sau khi Kafka ack. Nếu tiến trình chết giữa "Kafka đã nhận" và "đánh dấu processed", message sẽ được publish lại ở lần sau. Hệ quả: **có thể có duplicate** ở phía consumer.
- **Idempotent consumer (Inbox)** — Consumer pre-check Inbox rồi insert `(EventId)` trong transaction serializable với unique constraint. Một event dù được giao nhiều lần cũng chỉ tạo đúng một thay đổi nghiệp vụ; các lần sau trả về kết quả idempotent. Xem `InventoryTransactionBehavior`.

### Vì sao vẫn có duplicate?

At-least-once nghĩa là ranh giới giữa "đã gửi" và "đã ghi nhận đã gửi" không nguyên tử với transport. Crash hoặc retry ở đúng khoảng đó khiến message được giao lại. Đây là đánh đổi có chủ đích: thà giao lại (rồi khử trùng lặp ở consumer) còn hơn mất event.

### Vai trò của Outbox và Inbox

| Thành phần | Chống lại | Cơ chế |
| --- | --- | --- |
| Outbox | Mất event khi Kafka/publisher tạm lỗi | Ghi event cùng transaction nghiệp vụ; publish lại tới khi thành công; dead-letter sau `MaxAttempts` |
| Inbox | Xử lý nghiệp vụ trùng khi message bị giao lại | Pre-check + insert `EventId` trong transaction serializable; unique constraint là hàng rào cuối |

## Guarantee **không** được tuyên bố

- Hệ thống **không** cung cấp exactly-once delivery ở tầng transport.
- Hệ thống **không** đảm bảo tuyệt đối "không bao giờ mất event" trong mọi kịch bản hạ tầng (ví dụ mất toàn bộ storage đã commit). Đảm bảo chỉ áp dụng cho event đã commit vào Outbox và trong phạm vi các cơ chế mô tả ở trên.
- Không đảm bảo thứ tự global giữa các topic; xử lý stale/duplicate dựa trên version/state của aggregate.

## Scenario được kiểm chứng

| Scenario | Trạng thái | Test / thủ tục |
| --- | --- | --- |
| Outbox: lỗi publish tạm thời → retry → publish đúng một lần, không tạo bản ghi trùng | Automated (integration, Postgres) | `OutboxRetryReliabilityTests.Transient_publish_failure_is_retried_and_published_exactly_once` |
| Outbox: lỗi liên tục → tăng retry đúng → chuyển dead-letter theo `MaxAttempts`, không retry vô hạn | Automated (integration, Postgres) | `OutboxRetryReliabilityTests.Repeated_publish_failure_dead_letters_after_max_attempts` |
| Outbox: replay reset dead-letter về trạng thái retry được | Automated (integration, Postgres) | `OutboxReliabilityTests`, `InventoryPostgresReliabilityTests` |
| Consumer nhận duplicate delivery → chỉ đổi nghiệp vụ một lần | Automated (unit) | `InventoryTransactionBehaviorTests` (fast-path duplicate + race backstop) |
| Event cũ (stale) đến sau event mới → không ghi đè state mới | Automated (unit) | `ReserveStockHandlerTests` |
| AuditLog idempotency: cùng `EventId` chỉ tạo một audit document | Automated (integration, Mongo) | `MongoReliabilityTests` |
| Optimistic concurrency: hai confirm cùng ETag chỉ một cái tới Inventory | Automated (integration, Postgres) | `ConfirmOrderConcurrencyTests` |
| Consumer lỗi trước khi commit offset → Kafka redeliver → xử lý lại an toàn | Documented (manual) | xem [Thủ tục manual](#thủ-tục-manual-cần-live-kafka) |
| Process restart/crash recovery: message pending được tiếp tục sau restart | Documented (manual) | xem [Thủ tục manual](#thủ-tục-manual-cần-live-kafka) |

Các test unit chứng minh idempotency/stale nằm trong bộ **fast** (không cần hạ tầng). Các test gắn `[Trait("Category", "Reliability")]` là những test cần database thật.

## Cách chạy test

### Toàn bộ test nhanh (không cần hạ tầng)

```bash
dotnet test Sales.sln --configuration Release --filter "Category!=Reliability"
```

### Chỉ reliability suite (cần Postgres + Mongo)

Reliability tests bị gate bằng biến môi trường `RUN_RELIABILITY_TESTS=true`; khi không set, chúng no-op (return sớm) nên `dotnet test` mặc định vẫn xanh mà không cần database.

Khởi động hạ tầng (dùng luôn stack có sẵn hoặc container tạm):

```bash
docker run -d --name pg -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16
docker run -d --name mongo -p 27017:27017 mongo:7
```

Chạy:

```bash
export RUN_RELIABILITY_TESTS=true
export SALES_TEST_POSTGRES="Host=localhost;Port=5432;Database=sales_reliability_tests;Username=postgres;Password=postgres"
export INVENTORY_TEST_POSTGRES="Host=localhost;Port=5432;Database=inventory_reliability_tests;Username=postgres;Password=postgres"
export MONGO_TEST_CONNECTION="mongodb://localhost:27017"
export MONGO_TEST_DATABASE="audit_reliability_tests"

dotnet test Sales.sln --configuration Release --filter "Category=Reliability" \
  --logger "trx;LogFileName=reliability-tests.trx"
```

Các test tự tạo và migrate database riêng ở lần kết nối đầu (`Database.MigrateAsync()`), nên chỉ cần Postgres server chạy sẵn — không cần tạo database thủ công.

## CI workflow

`.github/workflows/ci.yml` tách thành hai job:

- **fast-checks** — chạy trên mọi push và pull request: restore, build (Release), `dotnet test --filter "Category!=Reliability"`, và validate Docker Compose.
- **reliability-tests** — chạy khi push vào `main` hoặc kích hoạt thủ công qua `workflow_dispatch`. Job này:
  - Khởi động service container Postgres 16 và Mongo 7 kèm healthcheck.
  - Set `RUN_RELIABILITY_TESTS=true` và các connection string tương ứng.
  - Chạy `dotnet test --filter "Category=Reliability"` với logger `trx`.
  - Khi thất bại: thu thập log các service container và upload cùng file `.trx` làm artifact.
  - Có `timeout-minutes` để tránh treo; không dùng `continue-on-error` cho bước test chính; không hard-code secret.

Kích hoạt thủ công: tab **Actions** → workflow **CI** → **Run workflow** (`workflow_dispatch`).

## Thủ tục manual (cần live Kafka)

Hai scenario dưới đây cần một Kafka thật cùng tiến trình consumer đang chạy, nên hiện được kiểm chứng bằng thủ tục thủ công thay vì test tự động (chưa đưa Kafka vào CI để giữ CI đơn giản và ổn định).

### Consumer lỗi trước khi commit offset

1. `docker compose -f docker/docker-compose.yml up -d`.
2. Gửi một lệnh tạo/confirm order để phát sinh event tới Inventory.
3. Trong lúc consumer đang xử lý, giả lập lỗi (ví dụ tạm dừng Postgres của Inventory hoặc kill consumer trước khi commit offset).
4. Khôi phục dependency; Kafka redeliver message.
5. Kỳ vọng: Inbox khử trùng lặp, stock không bị reserve/release hai lần; kiểm tra bảng `inbox_messages` và trạng thái reservation.

### Process restart / crash recovery

1. Tạo tải để có outbox row đang pending.
2. Restart `sales-api` (hoặc `inventory-api`) giữa chừng.
3. Sau restart, outbox publisher tiếp tục publish các row pending.
4. Kỳ vọng: không mất message (mọi row cuối cùng `ProcessedAt` hoặc dead-letter), không xử lý nghiệp vụ trùng nhờ Inbox.

Xem thêm `docs/tech/monitoring-demo.md` để quan sát trace/log/metric khi tái hiện các kịch bản này.
