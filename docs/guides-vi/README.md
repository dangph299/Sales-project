# Tài liệu học (Learning Guides)

Tài liệu mang tính giáo dục: vì sao từng phần của hệ thống tồn tại, nó hoạt động thế nào, và điều gì sẽ hỏng khi bạn làm "gần đúng" thay vì thực sự đúng.

Các guide này **giải thích**. Chúng không đặt ra quy định — muốn xem các quy tắc bắt buộc mà AI assistant và người review phải tuân theo, xem [../project/](../project/). Muốn xem dữ kiện thô (danh sách endpoint, schema, bảng topic), xem [../tech/](../tech/).

## Đọc theo thứ tự

| # | Guide | Bạn sẽ học được |
|---|---|---|
| 01 | [Tổng quan dự án](01-project-overview.md) | hệ thống làm gì và vì sao được chia thành ba context |
| 02 | [Cấu trúc solution](02-solution-structure.md) | từng project trong 16 project dùng để làm gì; file mới nên đặt ở đâu |
| 03 | [Vòng đời request](03-request-lifecycle.md) | một HTTP request đi từ socket xuống database rồi quay ra |
| 04 | [Xác thực & phân quyền](04-authentication-and-authorization.md) | phát hành JWT, xoay vòng refresh token, role, auth cho SignalR |
| 05 | [CQRS & pipeline MediatR](05-cqrs-and-mediatr.md) | vì sao read và write được mô hình hóa khác nhau |
| 06 | [DDD trong dự án này](06-ddd-in-this-project.md) | aggregate, value object, và những chỗ dự án này lệch chuẩn |
| 07 | [Domain Event & Transactional Outbox](07-domain-events-and-outbox.md) | làm sao một sự kiện trở thành message Kafka mà không mất tính nguyên tử |
| 08 | [Integration Event, Inbox & Retry](08-integration-events-and-inbox.md) | trùng lặp, thất bại, và event đến sai thứ tự |
| 09 | [Repository & Unit of Work](09-repository-and-unit-of-work.md) | hai đường ghi dữ liệu và ranh giới transaction |
| 10 | [Thiết kế database & migration](10-database-and-migrations.md) | mapping, filtered unique index, sinh mã tuần tự |
| 11 | [Caching với Redis](11-caching.md) | cache-aside dưới dạng decorator, và vì sao invalidation mới là phần khó |
| 12 | [Validation & xử lý lỗi](12-validation-and-error-handling.md) | ba lớp validation và một khuôn dạng lỗi duy nhất |
| 13 | [Observability](13-observability.md) | log, trace, metric — và cách trả lời một câu hỏi thực tế |
| 14 | [Background job & lập lịch](14-background-jobs.md) | bốn cơ chế và khi nào dùng cơ chế nào |
| 15 | [Concurrency & Idempotency](15-concurrency-and-idempotency.md) | bốn bài toán nhất quán, bốn lời giải khác nhau |
| 16 | [Kiến trúc frontend](16-frontend-architecture.md) | signal, một ranh giới HTTP duy nhất, không hardcode GUID |
| 17 | [Chiến lược test](17-testing-strategy.md) | cái gì được test ở đâu, và làm cho phần bất đồng bộ trở nên tất định |
| 18 | [Chạy & triển khai](18-running-and-deployment.md) | compose stack, CI, và những gì còn thiếu để lên production |

## Deep dive vận hành

Tài liệu dài hơn, thiên về thực hành, viết bằng tiếng Việt. Chúng đi sâu vào một công nghệ cụ thể hơn các chương ở trên và kèm lệnh troubleshooting chạy local.

| Guide | Nội dung |
|---|---|
| [configuration-guide.md](configuration-guide.md) | cấu hình khai báo ở đâu, bind vào code thế nào, đổi thì ảnh hưởng gì |
| [DDD-structure-guide.md](DDD-structure-guide.md) | bố cục DDD chi tiết, bao gồm cả những chỗ lệch chuẩn có chủ đích |
| [kafka-usage-guide.md](kafka-usage-guide.md) | mọi topic, producer, consumer và flow, kèm lệnh CLI |
| [kafka-playwright-debug-guide.md](kafka-playwright-debug-guide.md) | tái hiện và chẩn đoán một flow Sales↔Inventory bị kẹt |
| [Redis-cache-usage-guide.md](Redis-cache-usage-guide.md) | cache-aside, distributed lock, kiểm tra bằng `redis-cli` |
| [Seqlog-usage-guide.md](Seqlog-usage-guide.md) | thiết lập Serilog, enricher, correlation, truy vấn Seq |
| [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) | thiết lập SDK, custom metric, tracing xuyên Kafka |
| [Elastic-usage-guide.md](Elastic-usage-guide.md) | pipeline collector → APM → Elasticsearch → Kibana |

## Cấu trúc của mỗi chương

Mục đích → vấn đề → dự án này giải quyết thế nào → code lấy từ chính repository này → sơ đồ → lỗi thường gặp → tài liệu liên quan. Bảng "lỗi thường gặp" thường là phần hữu ích nhất; nó được rút ra từ chính những cảnh báo trong comment của code.
