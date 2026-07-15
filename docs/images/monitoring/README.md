# Monitoring screenshots

Thư mục này chứa screenshot bằng chứng cho Kibana dashboard và distributed trace.

**Hiện chưa có ảnh** — screenshot phải được chụp từ một lần chạy stack thật (Docker/Kibana), không dùng ảnh giả hoặc placeholder giả dạng ảnh thật.

## Ảnh cần chụp

| File (đề xuất) | Nội dung |
|---|---|
| `dashboard-overview.png` | Dashboard `Sales Management Reliability` sau khi có dữ liệu |
| `trace-cross-service.png` | Trace waterfall một `trace.id` đi Sales → Kafka → Inventory → Kafka → Sales (APM UI) |
| `trace-error-or-retry.png` | Trace lỗi (409 concurrency conflict) hoặc outbox backlog tăng khi Kafka dừng |

## Cách tạo

Xem [../../tech/monitoring-demo.md](../../tech/monitoring-demo.md):

1. Start stack và chờ `kibana-init` import dashboard.
2. Sinh dữ liệu demo + tái hiện retry.
3. Mở Kibana Dashboard/APM và chụp theo bảng trên.
4. Lưu ảnh vào thư mục này và nhúng vào `monitoring-demo.md`, ví dụ:

   ```markdown
   ![Dashboard overview](../images/monitoring/dashboard-overview.png)
   ```

Nguồn dashboard: `docker/kibana/exports/sales-management-reliability.ndjson`.
