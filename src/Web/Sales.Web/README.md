# Sales Web

FE nhỏ để test thủ công Sales/Inventory API trong bài thực hành DDD.

## Chạy

```bash
cd src/Web/Sales.Web
npm install
npm start
```

Mở `http://localhost:4200`.

Dev server proxy:

- `/sales-api` -> `http://localhost:5000`
- `/inventory-api` -> `http://localhost:5001`

Vì vậy hãy chạy stack backend trước:

```bash
sudo docker compose -f docker/docker-compose.yml up -d --build
```

## Test flow gợi ý

1. Health check.
2. Login bằng `admin` / `Admin123!`.
3. Create product và customer.
4. Adjust inventory cho product.
5. Create draft order.
6. Replace lines hoặc bấm `Test same ETag` để kiểm tra optimistic concurrency.
7. Confirm order, sau đó reload order/get inventory để xem reserve.
