# Logging Rules

## The one-log-per-failure rule

Each execution path logs its own failure **exactly once**, at its own boundary:

| Path | Logger |
|---|---|
| HTTP | `ApiExceptionHandler` |
| Kafka consume | `IntegrationEventHandler` |
| Outbox publish | `OutboxPublisherService` |
| Inbox re-drive | `InboxRedriveService` |
| Hangfire job | the job class |

`LoggingBehavior` deliberately logs at `Debug` only, including on failure. Never add Warning/Error logging to a MediatR behavior — it would double every failure in Seq and break error-rate counting.

## Structured logging

- Use message templates with named placeholders. Never string interpolation, never `+`.
- Placeholder names are PascalCase and stable — they are queried in Seq and Kibana.
- Standard property names: `TraceId`, `CorrelationId`, `RequestId`, `UserId`, `EventId`, `EventType`, `AggregateId`, `OrderId`, `Topic`, `GroupId`, `Partition`, `Offset`, `ElapsedMs`, `ErrorCode`, `StatusCode`.
- Do not invent a second name for a concept that already has one.

```csharp
logger.LogInformation("Order created {OrderId} {CustomerId}", order.Id, order.CustomerId);
```

## Levels

| Level | Use |
|---|---|
| `Debug` | pipeline breadcrumbs, request/response bodies, duplicate-event skips, EF SQL |
| `Information` | successful business milestones, publish/consume outcomes, job summaries |
| `Warning` | slow requests (`PerformanceBehavior`, ≥500 ms), conflicts, best-effort side effects that failed, re-drive failures |
| `Error` | 5xx, publish failures, dead-letter events, background cycle failures |

## Never log

- Passwords, tokens, secrets, connection strings — masked by `RequestObservabilityMiddleware` (`HttpLogging:SensitiveHeaders`, `SensitiveJsonFields`) and by `AuditOptions` for audit events.
- Full request payloads above `Debug`.
- PII in an `Information` message.

## Correlation

- `TraceId` = `Activity.Current.TraceId` hex, via `HttpContext.GetTraceId()`. This is the single definition for the whole solution.
- `CorrelationId` = the caller's `X-Correlation-Id` header, falling back to the trace id, via `HttpContext.GetCorrelationId()`.
- Both are pushed onto `LogContext` and `IDiagnosticContext` by `RequestObservabilityMiddleware`, so nested logs inherit them.
- Kafka consumers push `EventId`/`EventType`/`CorrelationId`/`TraceId` via `IMessageLogContext.Push(EventEnvelopeLogContext.From(envelope, activity))`.
- Never build correlation values ad hoc; use the shared helpers.

## Configuration

- Sinks are configured once in `SerilogBootstrap.ConfigureSharedSinks`: Console + Seq + OTLP. Do not add a sink in a service.
- Wire it with `builder.AddBuildingBlocksLogging("<service-name>")`.
- Levels come from the `Serilog` section in `appsettings.json`; EF SQL noise is suppressed with the `Microsoft.EntityFrameworkCore.Database.Command: Warning` override.
- `/health` and `/hangfire` are logged at `Debug` by `RequestLoggingDefaults` so uptime polling does not drown the signal.

## Related

- [exception-rule.md](exception-rule.md)
- Deep dive: [../../guides/Seqlog-usage-guide.md](../../guides/Seqlog-usage-guide.md)
