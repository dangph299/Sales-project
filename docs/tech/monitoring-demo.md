# Monitoring demo

Tài liệu này mô tả cách chứng minh log, trace và metric trước buổi demo: cách chạy stack, sinh dữ liệu, import Kibana dashboard, và tìm trace/log/metric tương ứng.

Repo có sẵn **Kibana dashboard export** (`docker/kibana/exports/sales-management-reliability.ndjson`) và được import tự động bởi service `kibana-init`. **Screenshot chưa được chụp** — cần chạy stack thật rồi chụp (xem [Screenshot](#screenshotdashboard)).

## Khởi động stack

Từ repository root:

```bash
docker compose -f docker/docker-compose.yml up --build
```

Các URL local theo `docker/docker-compose.yml`:

- Sales API: `http://localhost:5000`
- Inventory API: `http://localhost:5001`
- Seq: `http://localhost:8081`
- Elasticsearch: `http://localhost:9200`
- Kibana: `http://localhost:5601`
- APM Server: `http://localhost:8200`
- Kafka host endpoint: `localhost:9094`
- OTLP collector: `http://localhost:4318` hoặc gRPC `localhost:4317`

Nếu chỉ cần kiểm tra Kafka topic init:

```bash
docker compose -f docker/docker-compose.yml up -d kafka kafka-init
```

## Sinh log và trace

Một flow demo ngắn:

1. Login vào Sales API bằng user local `admin` / `Admin123!`.
2. Tạo hoặc cập nhật product/customer/order qua Sales API hoặc Angular test client.
3. Confirm order để Sales ghi DB và outbox.
4. Sales outbox publisher gửi Kafka event `sales.order-confirmation-requested.v1`.
5. Inventory consume event, ghi Inbox, reserve/reject stock và ghi Inventory outbox reply.
6. Sales consume reply để update order status.
7. Audit worker consume audit events và upsert MongoDB theo `EventId`.

## Tìm bằng chứng trong Seq

Mở `http://localhost:8081`, tìm theo các field structured log:

- `TraceId`: nối HTTP request, Kafka publish và Kafka consume khi trace context được propagate.
- `CorrelationId`: correlation nghiệp vụ trong `EventEnvelope`.
- `AggregateId`: id aggregate, thường là order id hoặc product/customer id.
- `EventId`: id message/event, dùng để đối chiếu Kafka/AuditLog.
- `Topic`: Kafka topic được publish/consume.
- `Partition` và `Offset`: vị trí message trong Kafka.

Ví dụ filter Seq:

```text
TraceId = '...'
CorrelationId = '...'
AggregateId = '...'
EventId = '...'
```

## Tìm trace trong Kibana/APM

Mở `http://localhost:5601`, vào Observability/APM hoặc Discover tùy setup Kibana local.

Kiểm tra các span chính:

- HTTP request span của Sales API hoặc Inventory API.
- PostgreSQL span khi handler đọc/ghi DB.
- Kafka producer span `kafka.publish <topic>`.
- Kafka consumer span `kafka.consume <topic>`.
- Exception/error trace nếu request bị validation error, concurrency conflict hoặc lỗi hạ tầng.

Không ghi rằng Kafka có global ordering: Kafka chỉ giữ thứ tự trong từng partition. Vì producer dùng `AggregateId` làm key và topic local có 3 partitions, các event cùng aggregate sẽ đi cùng partition để giữ ordering theo aggregate.

### Chứng minh request đi xuyên Sales → Kafka → Inventory → Kafka → Sales

1. Lấy `TraceId` (hoặc `CorrelationId`) từ log của request confirm order trong Seq.
2. Trong Kibana **Discover**, chọn data view `APM traces (Sales/Inventory)` (index `traces-apm*`) và filter `trace.id : "<TraceId>"` — hoặc dùng APM UI để xem waterfall.
3. Kỳ vọng thấy chuỗi span cùng `trace.id`: HTTP `sales-api` → `kafka.publish sales.order-confirmation-requested.v1` → consume ở `inventory-api` → xử lý + `kafka.publish inventory.stock-*` → consume trở lại ở `sales-api`.
4. Đối chiếu cùng `CorrelationId`/`EventId` trong Seq để xem log tương ứng từng chặng.

## Kibana dashboard: vị trí file và import

- **File export**: `docker/kibana/exports/sales-management-reliability.ndjson` (Kibana Saved Objects `_import` format — gồm 3 data view, 8 visualization và 1 dashboard `Sales Management Reliability`).
- **Import script**: `docker/kibana/import-dashboards.sh` — chờ Kibana available, POST `_import?overwrite=true`, idempotent, retry, log kết quả; nhận `KIBANA_URL`/`KIBANA_USERNAME`/`KIBANA_PASSWORD` qua env.
- **Tự động**: service one-shot `kibana-init` trong `docker/docker-compose.yml` chạy script sau khi Kibana khởi động. `docker compose up` sẽ tự import.
- **Import thủ công** (nếu cần chạy lại hoặc `kibana-init` bị tắt):

  ```bash
  KIBANA_URL=http://localhost:5601 docker/kibana/import-dashboards.sh
  ```

  Hoặc trong Kibana UI: **Stack Management → Saved Objects → Import** rồi chọn file `.ndjson`.
- Sau import, mở **Dashboard → Sales Management Reliability**. Nếu panel trống, mở **Data Views** và refresh field list cho `traces-apm*`/`metrics-apm*` (data view được tạo trước khi có dữ liệu nên field list rỗng cho tới khi APM ghi index đầu tiên).

> Lưu ý: file NDJSON được soạn thủ công theo schema Kibana 9.1 và **cần được xác nhận lại bằng một lần import thật** trên stack đang chạy; nếu một visualization cần chỉnh field, export lại từ Kibana để cập nhật file.

## Kịch bản lỗi để demo error trace

Một cách an toàn là gửi request sửa order với `If-Match` cũ:

1. User A và User B cùng đọc một order, nhận cùng ETag.
2. User A sửa order thành công, ETag tăng.
3. User B gửi request sửa order với ETag cũ.
4. API trả `409 Conflict`.
5. Tìm log/trace theo `TraceId` của request lỗi trong Seq và Kibana/APM.

Inventory serialization conflict/deadlock cũng được map thành `409` ở API, nhưng khó tạo ổn định trong demo thủ công; chỉ dùng khi có kịch bản concurrent request đáng tin cậy.

### Tái hiện retry / dead-letter của Outbox

1. Với stack đang chạy, tạm dừng Kafka: `docker compose -f docker/docker-compose.yml stop kafka`.
2. Confirm một order — business transaction commit, outbox row ở trạng thái pending, publish thất bại và retry.
3. Quan sát metric `sales.outbox.backlog` tăng (panel *Outbox health*) và log `Publish failed {EventId} {RetryCount}` trong Seq.
4. Khởi động lại Kafka: `docker compose -f docker/docker-compose.yml start kafka`. Publisher publish lại, backlog về 0, không tạo bản ghi trùng (Inbox khử trùng lặp phía consumer).
5. Nếu để lỗi kéo dài quá `MaxAttempts`, row chuyển dead-letter (`sales.outbox.deadletters` > 0); replay bằng `MaintenanceJobs.ReplayDeadLettersAsync`.

## Screenshot/dashboard

Screenshot **chưa được chụp trong môi trường này** vì không chạy được Docker/Kibana ở đây. Ảnh cần được chụp từ một lần chạy stack thật; **không commit ảnh giả hoặc placeholder giả dạng ảnh thật**.

- **Vị trí lưu**: `docs/images/monitoring/` (xem `README.md` trong thư mục đó để biết danh sách ảnh cần chụp và cách nhúng).
- Ảnh tối thiểu cần có:
  1. Dashboard tổng quan `Sales Management Reliability`.
  2. Trace waterfall xuyên service (một `trace.id` đi qua Sales → Kafka → Inventory → Kafka → Sales) trong APM.
  3. Trace lỗi/retry (409 concurrency conflict hoặc outbox backlog tăng khi Kafka dừng).

Các bước chụp:

1. `docker compose -f docker/docker-compose.yml up --build` và chờ `kibana-init` import xong (`docker compose logs kibana-init`).
2. Sinh dữ liệu theo mục [Sinh log và trace](#sinh-log-và-trace) và [tái hiện retry](#tái-hiện-retry--dead-letter-của-outbox).
3. Mở Kibana **Dashboard → Sales Management Reliability**, APM trace waterfall, và chụp theo danh sách trên.
4. Lưu ảnh vào `docs/images/monitoring/` và nhúng vào tài liệu này.
