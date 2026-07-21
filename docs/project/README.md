# AI Coding Knowledge

Rules for generating code in this repository. Written for Claude Code, Codex, and other coding agents.

These files are **prompts, not tutorials**. They state what to do and what never to do. Explanations live in [../guides/](../guides/); facts live in [../tech/](../tech/).

## How to use them

1. Read [backend/architecture.md](backend/architecture.md) and [backend/coding-rule.md](backend/coding-rule.md) (or the frontend equivalents) before writing anything.
2. Read the rule file for the concern you are touching.
3. Before claiming the work is done, walk [backend/checklist.md](backend/checklist.md) or [frontend/checklist.md](frontend/checklist.md).

If a rule and the existing code disagree, match the file you are editing and flag the conflict — consistency within a feature beats consistency with a document.

## Backend

| Concern | File |
|---|---|
| Layering, bounded contexts, composition | [architecture.md](backend/architecture.md) |
| Language, style, forbidden constructs | [coding-rule.md](backend/coding-rule.md) |
| Every naming convention | [naming.md](backend/naming.md) |
| Where files go | [folder-structure.md](backend/folder-structure.md) |
| Allowed references, ports, lifetimes | [dependency-rule.md](backend/dependency-rule.md) |
| DDD tactical patterns | [ddd-rule.md](backend/ddd-rule.md) |
| Aggregate shape and transitions | [aggregate-rule.md](backend/aggregate-rule.md) |
| Domain layer contents | [domain-rule.md](backend/domain-rule.md) |
| Entities and EF configuration | [entity-rule.md](backend/entity-rule.md) |
| Repositories and unit of work | [repository-rule.md](backend/repository-rule.md) |
| Commands, queries, pipeline | [cqrs-rule.md](backend/cqrs-rule.md) |
| DTOs, request/response models, mapping | [dto-rule.md](backend/dto-rule.md) |
| Validation layers | [validation-rule.md](backend/validation-rule.md) |
| Exceptions and error codes | [exception-rule.md](backend/exception-rule.md) |
| API design | [api-guideline.md](backend/api-guideline.md) |
| Controllers | [controller-rule.md](backend/controller-rule.md) |
| Database and queries | [database-rule.md](backend/database-rule.md) |
| Migrations | [migration-rule.md](backend/migration-rule.md) |
| Domain and integration events | [event-rule.md](backend/event-rule.md) |
| Kafka producing and consuming | [kafka-rule.md](backend/kafka-rule.md) |
| Caching and locks | [redis-rule.md](backend/redis-rule.md) |
| Logging | [logging-rule.md](backend/logging-rule.md) |
| Serialization | [serialization-rule.md](backend/serialization-rule.md) |
| Async and cancellation | [async-rule.md](backend/async-rule.md) |
| Performance | [performance-rule.md](backend/performance-rule.md) |
| Security | [security-rule.md](backend/security-rule.md) |
| Testing | [testing-rule.md](backend/testing-rule.md) |
| **Definition of done** | **[checklist.md](backend/checklist.md)** |

## Frontend

| Concern | File |
|---|---|
| Layering and feature structure | [architecture.md](frontend/architecture.md) |
| TypeScript and Angular style | [coding-rule.md](frontend/coding-rule.md) |
| Components | [component-rule.md](frontend/component-rule.md) |
| Signals and stores | [state-management.md](frontend/state-management.md) |
| HTTP and contracts | [api-rule.md](frontend/api-rule.md) |
| Where files go | [folder-structure.md](frontend/folder-structure.md) |
| Naming | [naming-rule.md](frontend/naming-rule.md) |
| ng-zorro and SCSS | [styling-rule.md](frontend/styling-rule.md) |
| Testing | [testing-rule.md](frontend/testing-rule.md) |
| **Definition of done** | **[checklist.md](frontend/checklist.md)** |

## The rules that matter most

If you read nothing else:

1. **Dependencies point inward.** Domain has no framework. Application declares ports; Infrastructure implements them.
2. **Invariants live in aggregates.** Not in handlers, validators, or controllers.
3. **Never publish to Kafka directly.** Domain event → mapper → outbox row → publisher.
4. **Never rethrow from a Kafka consumer handler.** The offset is already committed; record the failure in the inbox.
5. **Log a failure once, at its own boundary.**
6. **Never inline a topic, error code, queue name, or job id.** They are constants.
7. **Unique indexes on soft-deletable tables need `NOT "IsDelete"`.**
8. **Invalidate the cache after the save, never before.**
9. **No `.Result`, no `.Wait()`, no `DateTime.Now`.**
10. **Match the file you are editing.** Its conventions win over a general preference.
