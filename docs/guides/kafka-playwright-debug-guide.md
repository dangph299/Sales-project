# Debug flow Kafka Sales <-> Inventory bằng Playwright (và kiểm tra tay)

Guide này ghi lại quy trình thực tế đã dùng để tìm ra bug "order kẹt `PendingInventory`
không cố định" (xem root cause + fix ở [kafka-usage-guide.md](kafka-usage-guide.md) mục
"Bug đã fix: order kẹt PendingInventory sau cold start"). Dùng file này khi nghi ngờ
Kafka giữa Sales và Inventory không đáng tin cậy, và muốn tự kiểm tra lại bằng tay.

## 1. Khi nào dùng guide này

- Confirm order xong, `status` không chuyển từ `PendingInventory` sang
  `Confirmed`/`InventoryRejected`, hoặc chuyển rất chậm do consumer lag.
- Nghi ngờ hiện tượng "lúc được lúc không" (đặc biệt ngay sau khi mới start stack).
- Nghi flow `confirm -> undo confirm -> confirm` hoặc `confirm -> undo confirm -> update quantity > available -> confirm` bị kẹt.
- Muốn verify một fix liên quan Kafka consumer/outbox có thật sự hết flaky hay không.

## 2. Chuẩn bị

Stack phải đang chạy (Postgres, Kafka, Redis, Seq, `sales-api`, `inventory-api`):

```bash
sudo docker compose -f docker/docker-compose.yml ps
```

Cài Playwright deps (chỉ cần 1 lần):

```bash
cd tests/Playwright
npm install
```

Endpoint dùng trong guide này:

```text
Sales API:      http://localhost:5000
Inventory API:  http://localhost:5001
Seq:            http://localhost:8081
```

## 3. Bước 1 — Dùng Playwright để bắt flaky (tự động, ưu tiên làm trước)

Test có sẵn: [tests/Playwright/specs/kafka-flow.spec.ts](../tests/Playwright/specs/kafka-flow.spec.ts).
Nó tạo product + stock + customer + order, confirm order, rồi `expect.poll` tối đa 30s
chờ status thành `Confirmed`.

Chạy 1 lần:

```bash
cd tests/Playwright
npm run test:kafka
```

Các regression spec mới nên chạy khi sửa Inventory/Sales Kafka handler:

```bash
cd tests/Playwright
npx playwright test specs/reconfirm-flow.spec.ts
npx playwright test specs/over-available-after-undo.spec.ts
```

- `reconfirm-flow.spec.ts`: `create order -> confirm -> undo confirm -> confirm`, kỳ vọng cuối cùng `Confirmed`.
- `over-available-after-undo.spec.ts`: `create order -> confirm -> undo confirm -> update quantity > available -> confirm`, kỳ vọng cuối cùng `InventoryRejected`. Spec này dùng timeout dài hơn vì `StockRejected` có thể bị consumer Sales xử lý trễ trong local stack.

Chạy lặp lại nhiều lần để bắt flaky (test đơn lẻ không đủ để kết luận — bug này chỉ lộ
ra ở vài lần confirm **đầu tiên** sau khi consumer group là mới):

```bash
cd tests/Playwright
for i in $(seq 1 10); do
  echo "=== RUN $i ===";
  npx playwright test specs/kafka-flow.spec.ts --reporter=line 2>&1 | tail -20;
done
```

Đọc kết quả:

- `1 passed` → flow OK ở lần đó.
- `1 failed` kèm dòng `await expect .poll(...)` → order không bao giờ rời
  `PendingInventory` trong 30s. Đây chính là triệu chứng của bug.
- Nếu fail, xem trace để có thêm chi tiết request/response:

```bash
npx playwright show-trace test-results/kafka-flow-order-confirmat-ecf3e-hrough-Kafka-inventory-flow/trace.zip
```

**Quan trọng:** nếu vừa `docker compose up`/restart `sales-api`/`inventory-api` xong,
hãy chạy loop này **ngay lập tức** (trong vòng vài giây/phút đầu) — bug cold-start chỉ
lộ ra khi consumer group chưa có committed offset. Chạy loop sau khi stack đã "ấm"
(consumer đã xử lý vài event) sẽ luôn pass, không nói lên được gì.

## 4. Bước 2 — Repro bằng tay qua curl (khi cần kiểm soát chính xác timing)

Script dưới đây làm đúng việc Playwright test làm nhưng chạy bằng `curl`, in ra
`OrderId` và trạng thái sau mỗi giây — hữu ích khi muốn biết chính xác order nào bị kẹt
để tra tiếp ở Bước 3.

Lưu thành `repro.sh`, `chmod +x`, rồi chạy:

```bash
#!/usr/bin/env bash
set -euo pipefail
SALES=http://localhost:5000
INV=http://localhost:5001

TOKEN=$(curl -s -X POST "$SALES/api/auth/login" -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"Admin123!"}' \
  | python3 -c "import json,sys;print(json.load(sys.stdin)['accessToken'])")
AUTH="Authorization: Bearer $TOKEN"

RUNID="$(date +%s%N)"
SKU="REPRO-$RUNID"

PRODUCT=$(curl -s -X POST "$SALES/api/products/" -H "$AUTH" -H 'Content-Type: application/json' \
  -d "{\"sku\":\"$SKU\",\"name\":\"Repro $RUNID\",\"price\":100000}")
PID=$(echo "$PRODUCT" | python3 -c "import json,sys;print(json.load(sys.stdin)['id'])")

curl -s -X POST "$INV/api/inventory/$PID/adjust" -H "$AUTH" -H 'Content-Type: application/json' \
  -d "{\"sku\":\"$SKU\",\"quantityDelta\":10}" > /dev/null

PHONE="+849${RUNID: -8}"
CUSTOMER=$(curl -s -X POST "$SALES/api/customers/" -H "$AUTH" -H 'Content-Type: application/json' \
  -d "{\"name\":\"Repro Cust $RUNID\",\"phone\":\"$PHONE\"}")
CID=$(echo "$CUSTOMER" | python3 -c "import json,sys;print(json.load(sys.stdin)['id'])")

ORDER=$(curl -s -X POST "$SALES/api/orders/" -H "$AUTH" -H 'Content-Type: application/json' \
  -d "{\"customerId\":\"$CID\",\"lines\":[{\"productId\":\"$PID\",\"quantity\":2,\"discountPercent\":0}]}")
OID=$(echo "$ORDER" | python3 -c "import json,sys;print(json.load(sys.stdin)['id'])")

ETAG=$(curl -si -X GET "$SALES/api/orders/$OID" -H "$AUTH" | grep -i '^etag:' | sed 's/[Ee][Tt]ag: //I' | tr -d '\r')

CONFIRM_T0=$(date -u +%Y-%m-%dT%H:%M:%S.%3NZ)
curl -s -X POST "$SALES/api/orders/$OID/confirm" -H "$AUTH" -H "If-Match: $ETAG" > /dev/null

echo "OrderId=$OID ProductId=$PID ConfirmTime=$CONFIRM_T0"

for i in $(seq 1 30); do
  STATUS=$(curl -s "$SALES/api/orders/$OID" -H "$AUTH" | python3 -c "import json,sys;print(json.load(sys.stdin)['status'])")
  NOW=$(date -u +%H:%M:%S.%3N)
  echo "[$NOW] attempt $i status=$STATUS"
  if [ "$STATUS" = "Confirmed" ] || [ "$STATUS" = "InventoryRejected" ]; then
    echo "RESULT: reached terminal status $STATUS after $i polls"
    exit 0
  fi
  sleep 1
done
echo "RESULT: TIMEOUT, OrderId=$OID stuck, ConfirmTime=$CONFIRM_T0"
exit 1
```

Chạy vài lần liên tiếp để so sánh:

```bash
for i in 1 2 3 4 5 6; do echo "### iter $i ###"; ./repro.sh || true; echo; done
```

Nếu thấy `RESULT: TIMEOUT, OrderId=...`, ghi lại `OrderId` đó để tra ở Bước 3.

## 5. Bước 3 — Tìm order còn kẹt `PendingInventory` qua API

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"Admin123!"}' \
  | python3 -c "import json,sys;print(json.load(sys.stdin)['accessToken'])")

curl -s "http://localhost:5000/api/orders?pageSize=200" -H "Authorization: Bearer $TOKEN" \
  | python3 -c "
import json,sys
d=json.load(sys.stdin)
items = d.get('items', d if isinstance(d,list) else [])
stuck = [o for o in items if o['status'] not in ('Confirmed','InventoryRejected','Draft','Cancelled')]
print('stuck orders:', len(stuck))
for o in sorted(stuck, key=lambda x: x['createdAt']):
    print(o['createdAt'], o['status'], o['id'])
"
```

Order nào `createdAt` đã cách hiện tại nhiều phút mà vẫn `PendingInventory` cần đối chiếu
thêm outbox/inbox trước khi kết luận là kẹt vĩnh viễn. Local stack từng có case Inventory
publish `StockRejected` rồi Sales consume trễ hơn 30s; sau vài chục giây order vẫn chuyển
đúng sang `InventoryRejected`. Nếu vẫn kẹt sau nhiều phút, kiểm tra:

- Sales outbox đã publish `sales.order-confirmation-requested.v1` chưa.
- Inventory inbox đã nhận event confirm chưa.
- Inventory outbox có `inventory.stock-reserved.v1` hoặc `inventory.stock-rejected.v1` chưa.
- Sales inbox có nhận event kết quả từ Inventory chưa.
- Nếu inbox row có `Status = Failed`, kiểm tra `LastError`, `Attempts`, `NextAttemptAt`, `Payload`; event sẽ được `InventoryInboxRedriveService` hoặc `SalesInboxRedriveService` replay khi đến hạn.
- Nếu inbox row có `Status = DeadLettered`, sửa nguyên nhân trong `LastError` rồi reset row để redrive lại thay vì chờ Kafka gửi lại.
- Nếu Inventory đã ghi inbox cho confirm version mới nhưng không publish result, kiểm tra case event giữa confirm/undo đến lệch topic; code hiện dùng `Reservation.LastOrderVersion` để chặn release stale.

## 6. Bước 4 — Đối chiếu với Seq logs

Xem lỗi gần nhất liên quan Kafka:

```bash
curl -s "http://localhost:8081/api/events?count=50&filter=@Level%20%3D%20'Error'" \
  -H "Accept: application/json" | python3 -m json.tool | less
```

Đặc biệt chú ý pattern này — đây là dấu hiệu consumer group vừa cold start, đúng lúc dễ
mất event nhất:

```text
Confluent.Kafka.ConsumeException: Subscribed topic not available: <topic>: Broker: Unknown topic or partition
```

Log nghiệp vụ (Handling/Handled/failed) theo từng event nằm ở `SourceContext` của
`SalesIntegrationEventHandler`/`InventoryEventHandler` — filter theo `EventType` hoặc
`Topic` trong Seq UI (`http://localhost:8081`) nếu cần soi kỹ hơn theo từng order.

## 7. Verify riêng cho bug "AutoOffsetReset mặc định = latest" (đã fix)

Vì `auto.offset.reset` chỉ có tác dụng khi consumer group **chưa có committed offset**,
restart `sales-api`/`inventory-api` bình thường **không** đủ để verify fix — 2 consumer
group hiện tại (`sales-inventory-results-v1`, `inventory-orders-v1`) đã có offset từ lâu.

Muốn thấy race condition tái hiện (trước fix) hoặc xác nhận đã hết (sau fix), cần một
consumer group hoàn toàn mới:

```bash
# Xoá sạch data Kafka (và mọi service khác) — chỉ dùng ở môi trường local/test
sudo docker compose -f docker/docker-compose.yml down -v
sudo docker compose -f docker/docker-compose.yml up -d --build
```

Ngay sau khi `sales-api`/`inventory-api` báo healthy, chạy ngay Bước 3 (loop Playwright)
hoặc Bước 4 (curl loop) vài lần liên tiếp:

- **Trước fix**: 1-2 lần confirm đầu tiên timeout, order kẹt vĩnh viễn.
- **Sau fix** (`WithAutoOffsetReset(AutoOffsetReset.Earliest)` đã có trong
  `Sales.Infrastructure/DependencyInjection.cs` và
  `Inventory.Infrastructure/DependencyInjection.cs`): mọi lần confirm, kể cả lần đầu
  tiên ngay sau cold start, đều `Confirmed`/`InventoryRejected` trong vài giây.

## 8. Checklist tổng hợp

```text
[ ] Stack đang chạy (docker compose ps)
[ ] npm install trong tests/Playwright
[ ] Chạy loop kafka-flow.spec.ts 10 lần, đếm số fail
[ ] Chạy reconfirm-flow.spec.ts khi sửa confirm/undo confirm
[ ] Chạy over-available-after-undo.spec.ts khi sửa reject/stock flow
[ ] Nếu fail: lấy OrderId từ trace hoặc từ repro.sh
[ ] GET /api/orders để confirm order đó còn kẹt PendingInventory
[ ] Query Seq /api/events?filter=@Level='Error' quanh thời điểm đó
[ ] Nếu nghi AutoOffsetReset: down -v, up lại, test ngay lần đầu tiên
```
