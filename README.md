# Sales Management DDD practice

See [docs/architecture.md](docs/architecture.md) for dependency rules and folder responsibilities.
Use [docs/project-presentation.md](docs/project-presentation.md) to present the project, requirements mapping, Kafka flow, reliability patterns and demo flows.
Use [docs/Sumaries-guide.md](docs/Sumaries-guide.md) as the single entry point: requirements-vs-implementation status and known observability gaps.
Use [docs/kafka-usage-guide.md](docs/kafka-usage-guide.md) for a deeper explanation of KafkaFlow setup, producers, consumers, Outbox/Inbox and future reuse.
Use [docs/Redis-cache-usage-guide.md](docs/Redis-cache-usage-guide.md) for cache-aside and the distributed lock behind the Hangfire cleanup job.
Use [docs/Seqlog-usage-guide.md](docs/Seqlog-usage-guide.md) for Serilog/Seq setup per service, including where it's missing.
Use [docs/Elastic-usage-guide.md](docs/Elastic-usage-guide.md) for the OpenTelemetry -> OTel Collector -> APM Server -> Elasticsearch/Kibana pipeline.

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

Observability dashboard metrics are documented in [docs/observability.md](docs/observability.md).

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

## Angular test client

Một FE phụ trợ để test thủ công API nằm ở [src/Web/Sales.TestClient](src/Web/Sales.TestClient).

```bash
cd src/Web/Sales.TestClient
npm install
npm start
```

Mở `http://localhost:4200`, login bằng `admin` / `Admin123!`, rồi test product/customer/order/inventory/concurrency.
