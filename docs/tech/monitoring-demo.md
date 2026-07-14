# Monitoring demo

Tài liệu này mô tả cách chứng minh log, trace và metric trước buổi demo. Không có dashboard export hoặc screenshot thật trong repo hiện tại; chỉ thêm hướng dẫn chạy và kiểm tra.

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

## Kịch bản lỗi để demo error trace

Một cách an toàn là gửi request sửa order với `If-Match` cũ:

1. User A và User B cùng đọc một order, nhận cùng ETag.
2. User A sửa order thành công, ETag tăng.
3. User B gửi request sửa order với ETag cũ.
4. API trả `409 Conflict`.
5. Tìm log/trace theo `TraceId` của request lỗi trong Seq và Kibana/APM.

Inventory serialization conflict/deadlock cũng được map thành `409` ở API, nhưng khó tạo ổn định trong demo thủ công; chỉ dùng khi có kịch bản concurrent request đáng tin cậy.

## Screenshot/dashboard

Repo chưa có ảnh screenshot hoặc Kibana dashboard export thật. Nếu cần bằng chứng hình ảnh, hãy chạy flow trên, mở Kibana/Seq và chụp màn hình từ môi trường demo thật; không commit ảnh giả.
