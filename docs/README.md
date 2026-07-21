# Documentation

Three audiences, three folders. Pick the one that matches what you are doing.

| Folder | Audience | Answers |
|---|---|---|
| [guides/](guides/) | a human learning the project | *why* does this exist and *how* does it work? |
| [tech/](tech/) | anyone needing facts | *what* exists, *where* is it implemented? |
| [project/](project/) | AI coding assistants | *what rules* must generated code follow? |
| [superpowers/](superpowers/) | historical | design specs and plans — **read-only**, may be out of date |

Whenever documentation and code disagree, the code wins and the documentation gets fixed. Known gaps are recorded in [tech/discrepancies.md](tech/discrepancies.md).

## Start here

New to the repository? Read [guides/01-project-overview.md](guides/01-project-overview.md), then [guides/02-solution-structure.md](guides/02-solution-structure.md), then [guides/03-request-lifecycle.md](guides/03-request-lifecycle.md).

Generating code? Read [project/backend/architecture.md](project/backend/architecture.md) and [project/backend/coding-rule.md](project/backend/coding-rule.md), then the rule file for what you are touching, then verify against [project/backend/checklist.md](project/backend/checklist.md).

## guides/ — learning

| # | Guide |
|---|---|
| 01 | [Project Overview](guides/01-project-overview.md) |
| 02 | [Solution Structure](guides/02-solution-structure.md) |
| 03 | [Request Lifecycle](guides/03-request-lifecycle.md) |
| 04 | [Authentication & Authorization](guides/04-authentication-and-authorization.md) |
| 05 | [CQRS & the MediatR Pipeline](guides/05-cqrs-and-mediatr.md) |
| 06 | [DDD in This Project](guides/06-ddd-in-this-project.md) |
| 07 | [Domain Events & the Transactional Outbox](guides/07-domain-events-and-outbox.md) |
| 08 | [Integration Events, the Inbox & Retry](guides/08-integration-events-and-inbox.md) |
| 09 | [Repository & Unit of Work](guides/09-repository-and-unit-of-work.md) |
| 10 | [Database Design & Migrations](guides/10-database-and-migrations.md) |
| 11 | [Caching with Redis](guides/11-caching.md) |
| 12 | [Validation & Error Handling](guides/12-validation-and-error-handling.md) |
| 13 | [Observability](guides/13-observability.md) |
| 14 | [Background Jobs & Scheduling](guides/14-background-jobs.md) |
| 15 | [Concurrency & Idempotency](guides/15-concurrency-and-idempotency.md) |
| 16 | [Frontend Architecture](guides/16-frontend-architecture.md) |
| 17 | [Testing Strategy](guides/17-testing-strategy.md) |
| 18 | [Running & Deployment](guides/18-running-and-deployment.md) |

Plus operational deep dives (Vietnamese): [DDD structure](guides/DDD-structure-guide.md), [Kafka](guides/kafka-usage-guide.md), [Redis](guides/Redis-cache-usage-guide.md), [Seq](guides/Seqlog-usage-guide.md), [OpenTelemetry](guides/open-telemetry-usage-guide.md), [Elastic APM](guides/Elastic-usage-guide.md), [Kafka debugging with Playwright](guides/kafka-playwright-debug-guide.md).

## tech/ — knowledge base

**Business** — [Order lifecycle](tech/business/order-lifecycle.md) · [Inventory lifecycle](tech/business/inventory-lifecycle.md) · [Catalog rules](tech/business/catalog-rules.md) · [Customer rules](tech/business/customer-rules.md) · [Stock rules](tech/business/stock-rules.md) · [Pricing rules](tech/business/pricing-rules.md) · [Validation rules](tech/business/validation-rules.md)

**API** — [Conventions](tech/api-conventions.md) · [Endpoint reference](tech/api-endpoints.md) · [Errors & exceptions](tech/exception-and-error-catalog.md)

**Messaging** — [Conventions](tech/messaging-conventions.md) · [Topics & schemas](tech/kafka-topics-and-schemas.md) · [Outbox/Inbox schema](tech/outbox-inbox-schema.md) · [Retry & dead letter](tech/retry-and-dead-letter.md)

**Data** — [Database conventions](tech/database-conventions.md) · [Schema](tech/database-schema.md) · [Cache conventions](tech/cache-conventions.md)

**Platform** — [DI map](tech/dependency-injection-map.md) · [Configuration & environment](tech/configuration-and-environment.md) · [Background jobs](tech/background-jobs.md) · [Concurrency & idempotency](tech/concurrency-and-idempotency.md) · [Security](tech/security.md) · [Observability strategy](tech/logging-and-observability-strategy.md) · [Audit logging](tech/audit-logging.md) · [Monitoring demo](tech/monitoring-demo.md) · [Reliability tests](tech/reliability-tests.md)

**Orientation** — [Code map](tech/code-map.md) · [Architecture checklist](tech/architecture-checklist.md) · [Patterns guide](tech/patterns-guide.md) · [Glossary](tech/glossary.md) · [Requirements map](tech/requirements-map.md) · [Review notes](tech/review-notes.md) · [Discrepancies](tech/discrepancies.md)

## project/ — AI coding rules

**Backend** — [architecture](project/backend/architecture.md) · [coding-rule](project/backend/coding-rule.md) · [naming](project/backend/naming.md) · [folder-structure](project/backend/folder-structure.md) · [dependency-rule](project/backend/dependency-rule.md) · [ddd-rule](project/backend/ddd-rule.md) · [aggregate-rule](project/backend/aggregate-rule.md) · [domain-rule](project/backend/domain-rule.md) · [entity-rule](project/backend/entity-rule.md) · [repository-rule](project/backend/repository-rule.md) · [cqrs-rule](project/backend/cqrs-rule.md) · [dto-rule](project/backend/dto-rule.md) · [validation-rule](project/backend/validation-rule.md) · [exception-rule](project/backend/exception-rule.md) · [api-guideline](project/backend/api-guideline.md) · [controller-rule](project/backend/controller-rule.md) · [database-rule](project/backend/database-rule.md) · [migration-rule](project/backend/migration-rule.md) · [event-rule](project/backend/event-rule.md) · [kafka-rule](project/backend/kafka-rule.md) · [redis-rule](project/backend/redis-rule.md) · [logging-rule](project/backend/logging-rule.md) · [serialization-rule](project/backend/serialization-rule.md) · [async-rule](project/backend/async-rule.md) · [performance-rule](project/backend/performance-rule.md) · [security-rule](project/backend/security-rule.md) · [testing-rule](project/backend/testing-rule.md) · **[checklist](project/backend/checklist.md)**

**Frontend** — [architecture](project/frontend/architecture.md) · [coding-rule](project/frontend/coding-rule.md) · [component-rule](project/frontend/component-rule.md) · [state-management](project/frontend/state-management.md) · [api-rule](project/frontend/api-rule.md) · [folder-structure](project/frontend/folder-structure.md) · [naming-rule](project/frontend/naming-rule.md) · [styling-rule](project/frontend/styling-rule.md) · [testing-rule](project/frontend/testing-rule.md) · **[checklist](project/frontend/checklist.md)**

## Keeping these current

| You changed | Update |
|---|---|
| a business rule | `tech/business/` |
| an endpoint | `tech/api-endpoints.md` |
| a Kafka topic or payload | `tech/kafka-topics-and-schemas.md` |
| the schema | `tech/database-*.md` |
| a registration | `tech/dependency-injection-map.md` |
| a configuration key | `tech/configuration-and-environment.md` |
| a convention or standard | the matching `project/` rule file |
| how something works conceptually | the matching `guides/` chapter |
| discovered or fixed a gap | `tech/discrepancies.md` |
