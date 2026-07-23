# Learning Guides

Educational documentation: why each part of the system exists, how it works, and what goes wrong when you get it subtly right instead of actually right.

These guides explain. They do not prescribe — for rules that AI assistants and reviewers must follow, see [../project/](../project/). For raw facts (endpoint lists, schemas, topic tables), see [../tech/](../tech/).

## Read in order

| # | Guide | You will learn |
|---|---|---|
| 01 | [Project Overview](01-project-overview.md) | what the system does and why it is split into three contexts |
| 02 | [Solution Structure](02-solution-structure.md) | what each project is for; where a new file goes |
| 03 | [Request Lifecycle](03-request-lifecycle.md) | one HTTP request from socket to database and back |
| 04 | [Authentication & Authorization](04-authentication-and-authorization.md) | JWT issuance, refresh rotation, roles, SignalR auth |
| 05 | [CQRS & the MediatR Pipeline](05-cqrs-and-mediatr.md) | why reads and writes are modelled differently |
| 06 | [DDD in This Project](06-ddd-in-this-project.md) | aggregates, value objects, and where this project deviates |
| 07 | [Domain Events & the Transactional Outbox](07-domain-events-and-outbox.md) | how a fact becomes a Kafka message without losing atomicity |
| 08 | [Integration Events, the Inbox & Retry](08-integration-events-and-inbox.md) | duplicates, failures, and out-of-order arrival |
| 09 | [Repository & Unit of Work](09-repository-and-unit-of-work.md) | two persistence paths and the transaction boundary |
| 10 | [Database Design & Migrations](10-database-and-migrations.md) | mapping, filtered unique indexes, code sequences |
| 11 | [Caching with Redis](11-caching.md) | cache-aside as a decorator, and why invalidation is the hard half |
| 12 | [Validation & Error Handling](12-validation-and-error-handling.md) | three validation layers and one error shape |
| 13 | [Observability](13-observability.md) | logs, traces, metrics — and how to answer a real question |
| 14 | [Background Jobs & Scheduling](14-background-jobs.md) | four mechanisms and when each is right |
| 15 | [Concurrency & Idempotency](15-concurrency-and-idempotency.md) | four consistency problems, four different answers |
| 16 | [Frontend Architecture](16-frontend-architecture.md) | signals, one HTTP boundary, no hardcoded GUIDs |
| 17 | [Testing Strategy](17-testing-strategy.md) | what is tested where, and making async work deterministic |
| 18 | [Running & Deployment](18-running-and-deployment.md) | the compose stack, CI, and what is missing for production |

## Operational deep dives

Longer, hands-on references written in Vietnamese. They go deeper on one technology than the chapters above and include local troubleshooting commands.

| Guide | Covers |
|---|---|
| [DDD-structure-guide.md](DDD-structure-guide.md) | the DDD layout in detail, including deliberate deviations |
| [kafka-usage-guide.md](kafka-usage-guide.md) | every topic, producer, consumer, and flow, plus CLI commands |
| [kafka-playwright-debug-guide.md](kafka-playwright-debug-guide.md) | reproducing and diagnosing a stuck Sales↔Inventory flow |
| [Redis-cache-usage-guide.md](Redis-cache-usage-guide.md) | cache-aside, the distributed lock, `redis-cli` checks |
| [Seqlog-usage-guide.md](Seqlog-usage-guide.md) | Serilog setup, enrichers, correlation, querying Seq |
| [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) | SDK setup, custom metrics, tracing across Kafka |
| [Elastic-usage-guide.md](Elastic-usage-guide.md) | the collector → APM → Elasticsearch → Kibana pipeline |

## Structure of each chapter

Purpose → the problem → how this project solves it → code from this repository → diagrams → common mistakes → related documents. The "common mistakes" table is usually the most useful part; it is drawn from what the code's own comments warn about.
