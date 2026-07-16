# Sales Management DDD practice

Start with [docs/tech/README.md](docs/tech/README.md) if you want a beginner-friendly explanation of the project requirements, patterns, code locations, and review notes.
See [docs/architecture.md](docs/architecture.md) for dependency rules and folder responsibilities.
See [docs/CODING_RULES.md](docs/CODING_RULES.md) for coding and placement rules.
See [docs/ARCHITECTURE_CHECKLIST.md](docs/ARCHITECTURE_CHECKLIST.md) for the current architecture checklist.

Greenfield .NET 10 sample with a Sales modular monolith, an Inventory service and an Audit worker. Commands use MediatR and aggregate repositories; reads use EF Core projections. Order/inventory integration is delivered at least once through transactional Outbox/Inbox records and KafkaFlow.

## Run locally

```bash
docker compose -f docker/docker-compose.yml up --build
```

- Sales Swagger UI: `http://localhost:5000/swagger`
- Inventory OpenAPI: `http://localhost:5001/openapi/v1.json`
- Seq: `http://localhost:8081`
- Kibana: `http://localhost:5601`
- Kafka host endpoint: `localhost:9094`

Observability and monitoring entry points are summarized in [docs/tech/requirements-map.md](docs/tech/requirements-map.md); use [docs/tech/monitoring-demo.md](docs/tech/monitoring-demo.md) for the demo flow.

The development admin is `admin` / `Admin123!`. Change this and the JWT key outside local development.

## Main workflow

1. Log in at `POST /api/auth/login`.
2. Create products and customers, then create a draft order.
3. Use the response `ETag` in `If-Match` when editing or confirming an order.
4. Confirmation writes an Outbox event. Inventory reserves stock idempotently and emits a success or rejection event.
5. Sales updates the order state; Audit.Worker stores every integration event in MongoDB with a unique `eventId`.

Phone values are normalized to digits. Search supports name, phone prefix/suffix and UTC order ranges (`from` inclusive, `to` exclusive). Prices are VND rounded to zero decimal places with `AwayFromZero`.

## Verify

```bash
dotnet test Sales.sln
docker compose -f docker/docker-compose.yml config
```

Reliability/integration tests that touch real Postgres and Mongo are opt-in:

```bash
RUN_RELIABILITY_TESTS=true dotnet test Sales.sln
```

Default local endpoints are `localhost:5432` and `localhost:27017`; override with `SALES_TEST_POSTGRES`, `INVENTORY_TEST_POSTGRES`, `MONGO_TEST_CONNECTION`, and `MONGO_TEST_DATABASE`.

## Sales web

Một FE phụ trợ để test thủ công API nằm ở [src/Web/Sales.Web](src/Web/Sales.Web).

```bash
cd src/Web/Sales.Web
npm install
npm start
```

Mở `http://localhost:4200`, login bằng `admin` / `Admin123!`, rồi test product/customer/order/inventory/concurrency.
