# Tài liệu kỹ thuật cho dự án DDD

Thư mục này giải thích các yêu cầu và pattern trong dự án theo cách dễ đọc cho người mới. Nếu bạn chưa biết DDD, CQRS, Outbox, Kafka, Redis cache hoặc Hangfire, hãy đọc theo thứ tự dưới đây.

## Nên đọc theo thứ tự nào?

1. [requirements-map.md](requirements-map.md)
   - Đối chiếu từng yêu cầu bài thực hành với code thực tế.
   - Chỉ rõ yêu cầu đã đáp ứng ở đâu.
   - Ghi chú các điểm cần lưu ý khi dùng hoặc mở rộng.

2. [patterns-guide.md](patterns-guide.md)
   - Giải thích từng pattern là gì.
   - Dự án này khai báo và sử dụng pattern đó như thế nào.
   - Khi thêm code mới cần đặt file ở đâu và tránh lỗi gì.

3. [code-map.md](code-map.md)
   - Bản đồ thư mục theo layer và bounded context.
   - Luồng chạy chính: tạo đơn hàng, xác nhận đơn hàng, Inventory giữ hàng, AuditLog ghi MongoDB.

4. [review-notes.md](review-notes.md)
   - Tổng hợp kết quả review.
   - Các phần đã đạt.
   - Các rủi ro cần sửa hoặc cần theo dõi.

5. [inventory-cqrs-refactor-audit.md](inventory-cqrs-refactor-audit.md)
   - Báo cáo audit và mapping refactor Inventory sang CQRS/MediatR.
   - Ghi rõ component shared được tái sử dụng, abstraction đặc thù được tạo, và code cũ đã xóa.

6. [audit-logging.md](audit-logging.md)
   - Kiến trúc audit hybrid: EF Core ChangeTracker, enricher, Outbox, Kafka và MongoDB.
   - Cách thêm audit cho entity CRUD mới hoặc hành động nghiệp vụ đặc biệt.

7. [monitoring-demo.md](monitoring-demo.md)
   - Cách chạy stack Seq/Elasticsearch/Kibana/APM/OpenTelemetry.
   - Cách sinh request, tìm trace/log theo TraceId, CorrelationId, AggregateId hoặc EventId.

8. [glossary.md](glossary.md)
   - Bảng thuật ngữ ngắn cho người mới.

## Dự án này đang theo kiến trúc nào?

Dự án là hệ thống quản lý bán hàng theo hướng DDD và Clean Architecture, tách thành các bounded context:

- Sales: sản phẩm, khách hàng, đơn hàng.
- Inventory: tồn kho và giữ hàng cho đơn.
- AuditLog: consume audit topics và ghi audit trail vào MongoDB.
- Shared BuildingBlocks: các thành phần dùng chung như domain base type, CQRS marker, outbox, audit, logging, OpenTelemetry.

Hướng phụ thuộc chính:

```text
Api/Worker -> Infrastructure -> Application -> Domain
```

Domain không được phụ thuộc EF Core, Kafka, Redis, HTTP, Hangfire hoặc MongoDB. Những phần đó thuộc Infrastructure hoặc Api/Worker.

## File quan trọng nên biết

- [../architecture.md](../architecture.md): quy ước layer và trách nhiệm thư mục.
- [../CODING_RULES.md](../CODING_RULES.md): quy tắc code bắt buộc.
- [../ARCHITECTURE_CHECKLIST.md](../ARCHITECTURE_CHECKLIST.md): checklist hiện trạng kiến trúc.
- [../../README.md](../../README.md): cách chạy dự án.
