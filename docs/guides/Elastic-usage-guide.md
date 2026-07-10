# Elastic APM usage guide trong Sales Management

Tài liệu này giải thích riêng phần hạ tầng **phía sau** OpenTelemetry: OTel Collector → Elastic APM Server → Elasticsearch → Kibana — pipeline cấu hình ra sao, service nào start trước, và giới hạn thật của setup hiện tại. Phần **code trong app** dùng OpenTelemetry SDK (khởi tạo tracing/metrics, custom metric, ActivitySource cho Kafka, log OTLP...) đã tách sang [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) — đọc tài liệu đó trước nếu bạn cần biết "app gọi API OTel như thế nào", đọc tài liệu này nếu bạn cần biết "dữ liệu đó đi đâu và Kibana dựng ra sao". Danh sách panel Kibana cần dựng nằm ở [observability.md](observability.md). Cùng phong cách với [kafka-usage-guide.md](kafka-usage-guide.md).

## Tóm tắt nhanh

- Pipeline là **thuần OTLP end-to-end**: project không dùng Elastic APM native agent, mà dùng APM Server 9.1.0 làm "translator" nhận OTLP rồi ghi vào Elasticsearch.
- 3 signal cùng đi qua 1 pipeline: **traces + metrics + logs**. Trước đây log tách biệt hoàn toàn (chỉ Console/Seq); giờ Serilog ghi thêm 1 nhánh OTLP nữa (chi tiết ở [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 6), nên `otel-collector-config.yaml` có cả pipeline `logs`.
- `auth.anonymous.enabled: true` ở APM Server và không bật `xpack.security` ở Elasticsearch — chấp nhận được cho local MVP, **không dùng khi lên production** (mục 6, 9).
- Thứ tự start: `elasticsearch → kibana → apm-server → otel-collector`; app không phụ thuộc cứng vào collector nên vẫn start được nếu observability stack chưa sẵn sàng.

## 1. Kiến trúc pipeline tổng thể

```text
Sales.Api / Inventory.Api / AuditLog.Worker
    --OTLP gRPC (4317), traces + metrics + logs-->
otel-collector (receivers.otlp -> processors[memory_limiter, batch] -> exporters.otlp/elastic)
    --OTLP over HTTP (endpoint apm-server:8200, TLS insecure)-->
apm-server (auth.anonymous enabled)
    --output.elasticsearch-->
elasticsearch:9200
    <-- query --
kibana:5601
```

Đây là pipeline **thuần OTLP end-to-end**. Project **không** dùng Elastic APM native agent (`Elastic.Apm.NetCoreAll`/`Elastic.Apm.AspNetCore`) — không có package đó trong bất kỳ `.csproj` nào. APM Server 9.1.0 tự hiểu giao thức OTLP nên đóng vai trò "translator" từ OTLP sang Elasticsearch data stream, không phải nhận traffic từ Elastic APM agent kiểu cũ.

**Cả 3 signal (traces, metrics, logs) đều đi qua pipeline này.** Serilog (Console + Seq, xem [Seqlog-usage-guide.md](Seqlog-usage-guide.md)) vẫn là kênh log chính để tra cứu thủ công hằng ngày, nhưng từ khi `BuildingBlocks.Observability.SerilogBootstrap` thêm `WriteTo.OpenTelemetry(...)`, mỗi log event cũng được gửi song song qua OTLP → collector → APM Server → Elasticsearch. Chi tiết SDK phía app nằm ở [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 6.

## 2. OpenTelemetry SDK khởi tạo trong từng service

Đã tách sang [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 2–3 (package, code khởi tạo `AddOpenTelemetry()` của Sales.Api/Inventory.Api/AuditLog.Worker). Tóm tắt 1 dòng: cả 3 service `AddOpenTelemetry().WithTracing(...).WithMetrics(...)`, export qua `AddOtlpExporter()` không hard-code endpoint (mục 3 dưới đây).

## 3. Exporter cấu hình qua biến môi trường, không hard-code trong code

`AddOtlpExporter()` không truyền endpoint/protocol trong code — cấu hình đến từ biến môi trường trong `docker/docker-compose.yml`:

```yaml
environment:
  OTEL_EXPORTER_OTLP_ENDPOINT: http://otel-collector:4317
  OTEL_SERVICE_NAME: sales-api   # inventory-api / audit-worker tương ứng
```

`OTEL_SERVICE_NAME` chính là field `service.name` xuất hiện trong Kibana/APM (khớp cột "Service" trong bảng dashboard ở `observability.md`), và cũng là property `Service` xuất hiện trong log Seq (cùng 1 biến môi trường, dùng chung cho cả 2 kênh — xem [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 8). Cổng `4317` là gRPC — .NET OTLP exporter mặc định dùng gRPC khi không set `OTEL_EXPORTER_OTLP_PROTOCOL`.

## 4. Custom metric — SalesMetrics / InventoryMetrics

Đã tách sang [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 4 (định nghĩa `Meter`/`Counter`/`ObservableGauge`, bảng nơi tăng counter). Danh sách tên metric hiện có, dùng để dựng panel Kibana, xem [observability.md](observability.md).

## 5. OTel Collector pipeline

`docker/otel-collector-config.yaml`:

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318
processors:
  batch: {}
  memory_limiter:
    check_interval: 1s
    limit_mib: 256
exporters:
  otlp/elastic:
    endpoint: apm-server:8200
    tls:
      insecure: true
service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [otlp/elastic]
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [otlp/elastic]
    logs:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [otlp/elastic]
```

3 pipeline (`traces`, `metrics`, `logs`) dùng chung 1 receiver và 1 exporter — collector không phân biệt xử lý gì đặc biệt theo signal, chỉ route cả 3 tới cùng APM Server. Cả 2 cổng gRPC (4317) và HTTP (4318) đều mở, nhưng app chỉ dùng gRPC theo biến môi trường ở mục 3. `memory_limiter` + `batch` là processor chuẩn khuyến nghị của OTel Collector để tránh OOM và giảm số request gửi đi. `tls.insecure: true` chấp nhận được vì toàn bộ traffic nằm trong mạng Docker nội bộ, không ra ngoài.

## 6. APM Server config

`docker/apm-server.yml`:

```yaml
apm-server:
  host: 0.0.0.0:8200
  auth:
    anonymous:
      enabled: true
output.elasticsearch:
  hosts: ["http://elasticsearch:9200"]
setup.kibana:
  host: "kibana:5601"
```

- `auth.anonymous.enabled: true`: chấp nhận data không cần API key — phù hợp local MVP, **không phù hợp production**.
- `output.elasticsearch`: APM Server ghi thẳng vào Elasticsearch (index `traces-apm-*`, `metrics-apm-*`, và giờ có thêm `logs-apm-*`/`logs-*` nhờ pipeline `logs` ở mục 5).
- `setup.kibana`: cho phép APM Server tự tạo index pattern/asset trong Kibana lúc khởi động.

## 7. Docker Compose — service và thứ tự khởi động

`docker/docker-compose.yml`:

```yaml
elasticsearch:
  image: docker.elastic.co/elasticsearch/elasticsearch:9.1.0
  environment:
    discovery.type: single-node
    xpack.security.enabled: "false"
    ES_JAVA_OPTS: -Xms512m -Xmx512m
  ports: ["9200:9200"]
  volumes: ["elastic-data:/usr/share/elasticsearch/data"]

kibana:
  image: docker.elastic.co/kibana/kibana:9.1.0
  environment:
    ELASTICSEARCH_HOSTS: http://elasticsearch:9200
  ports: ["5601:5601"]
  depends_on: [elasticsearch]

apm-server:
  image: docker.elastic.co/apm/apm-server:9.1.0
  command: ["--strict.perms=false", "-e", "-c", "/usr/share/apm-server/apm-server.yml"]
  volumes: ["./apm-server.yml:/usr/share/apm-server/apm-server.yml:ro"]
  ports: ["8200:8200"]
  depends_on: [elasticsearch, kibana]

otel-collector:
  image: otel/opentelemetry-collector-contrib:0.135.0
  command: ["--config=/etc/otelcol/config.yaml"]
  volumes: ["./otel-collector-config.yaml:/etc/otelcol/config.yaml:ro"]
  ports: ["4317:4317", "4318:4318"]
  depends_on: [apm-server]
```

Thứ tự start (`depends_on` chỉ đảm bảo container order, không chờ health): `elasticsearch → kibana → apm-server → otel-collector`. `sales-api`/`inventory-api`/`audit-worker` **không** khai `depends_on: otel-collector` — app có thể start trước khi collector sẵn sàng; export OTLP thất bại thì lặng lẽ retry/rớt, không crash app (áp dụng cho cả 3 signal, kể cả log OTLP sink — Console/Seq vẫn nhận log bình thường dù nhánh OTLP rớt).

## 8. Trace field hữu ích khi tra cứu Kibana

Xem thêm bảng panel đầy đủ ở [observability.md](observability.md). Field trace hay dùng:

```text
service.name
trace.id
http.route
messaging.destination.name
messaging.kafka.consumer.group
db.system
```

`messaging.destination.name`/`messaging.kafka.consumer.group` được set thủ công trên span Kafka publish/consume ([open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 5). `db.system` xuất hiện tự động qua `AddEntityFrameworkCoreInstrumentation()`.

## 9. Distributed tracing xuyên qua Kafka

Chi tiết `ActivitySource`, W3C `traceparent`/`tracestate` header, `TraceContextParser` dùng chung cho cả 3 consumer handler — đã tách hoàn toàn sang [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 5, vì đây là phần **code trong app**, không phải hạ tầng Elastic. Tài liệu này chỉ quan tâm: span đó (dù tạo thủ công) vẫn đi đúng qua OTLP pipeline ở mục 1 như mọi span khác, không cần cấu hình riêng phía Collector/APM Server.

## 10. Đã cải thiện / còn lại

Đã fix trong code (không còn là khuyến nghị):

- ✅ EF Core instrumentation — query DB có span trong trace ([open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 2–3).
- ✅ `ActivitySource` thủ công quanh Kafka produce/consume, W3C `traceparent`/`tracestate` propagate qua Kafka header thật, dùng chung `TraceContextParser` ([open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 5).
- ✅ Log giờ cũng đi qua OTLP pipeline (`WriteTo.OpenTelemetry(...)` trong `SerilogBootstrap`), `otel-collector-config.yaml` có thêm pipeline `logs` ở mục 5 — trước đây log hoàn toàn tách biệt.

Còn lại, chưa fix:

- Bật `auth` cho APM Server (bỏ `anonymous.enabled: true`) và bật `xpack.security` cho Elasticsearch khi lên production.
- Thêm custom metric cho Redis cache hit/miss (hiện chưa có — xem [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 9 cho hướng dẫn thêm, [Redis-cache-usage-guide.md](Redis-cache-usage-guide.md) mục 9).
- Dựng dashboard Kibana theo đúng danh sách panel ở [observability.md](observability.md) nếu chưa dựng thủ công.
- Propagate `traceparent` từ HTTP request gốc xuyên outbox row tới lúc publish thật ([open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 5.4) — cần đổi schema outbox, ngoài phạm vi hiện tại.
- Sampling cho traces/logs khi tải cao ([open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 10).
