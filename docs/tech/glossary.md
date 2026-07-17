# Bảng thuật ngữ

Bảng này giải thích nhanh các từ hay gặp trong dự án.

| Từ | Giải thích ngắn | Ví dụ trong repo |
|---|---|---|
| Bounded Context | Ranh giới nghiệp vụ riêng, có model và rule riêng | Sales, Inventory, AuditLog |
| Domain | Nơi chứa rule nghiệp vụ cốt lõi | `Sales.Domain`, `Inventory.Domain` |
| Aggregate Root | Object chính bảo vệ tính nhất quán cho một cụm dữ liệu | `Order`, `Product`, `Customer`, `Reservation` |
| Entity | Object có identity | `OrderLine`, `InventoryItem` |
| Value Object | Object không có identity riêng, so sánh theo giá trị | `Money`, `ProductSnapshot` |
| Domain Event | Sự kiện nội bộ domain | `OrderConfirmationRequestedDomainEvent` |
| Integration Event | Sự kiện gửi giữa các service | `OrderConfirmationRequested`, `StockReserved` |
| CQRS | Tách command ghi dữ liệu và query đọc dữ liệu | `Features/<Aggregate>/Commands/`, `Features/<Aggregate>/Queries/` trong Sales.Application |
| Command | Request làm thay đổi state | `CreateOrder`, `ConfirmOrder` |
| Query | Request chỉ đọc dữ liệu | `SearchOrders`, `GetProduct` |
| Handler | Class xử lý command/query | `CreateOrderHandler` |
| MediatR | Thư viện dispatch command/query tới handler | `ISender.Send(...)` |
| Repository | Cổng truy cập aggregate ở command-side | `IOrderRepository`, `Repository<T>` |
| Unit of Work | Gom nhiều thay đổi và commit một lần | `IUnitOfWork.SaveChangesAsync` |
| DTO | Object dùng để trả/nhận data qua API hoặc read side | `OrderDto`, `ProductDto` |
| Mapster | Thư viện mapping object sang DTO | `ProductMappingRegister`, `OrderMappingRegister` |
| Outbox | Bảng tạm lưu event cần publish Kafka | `OutboxMessage` |
| Inbox | Bảng lưu event đã consume để tránh xử lý duplicate | `InboxMessage` (dùng chung Sales/Inventory) |
| Kafka Topic | Kênh message | `sales.order-confirmation-requested.v1` |
| Consumer Group | Nhóm consumer cùng xử lý topic | `inventory-orders-v1` |
| Partition | Phần chia nhỏ của topic trong Kafka | partition/offset trong log Kafka |
| Redis Cache | Cache dữ liệu đọc nhanh hơn DB | `ProductCache` |
| Distributed Lock | Lock dùng chung giữa nhiều instance | Redis lock trong Sales cleanup, Postgres advisory lock trong Inventory cleanup |
| Hangfire | Thư viện chạy background job | Sales cleanup recurring job |
| Optimistic Concurrency | Phát hiện conflict bằng version | ETag/If-Match trên order |
| ETag | Version của resource trả qua HTTP header | `Response.SetEtag(order)` |
| OpenTelemetry | Chuẩn trace/metric/log | `AddBuildingBlocksObservability` / `AddBuildingBlocksWebObservability` |
| Seq | Hệ thống xem log | docker service `seq` |
| Kibana | UI cho Elasticsearch/APM | docker service `kibana` |
