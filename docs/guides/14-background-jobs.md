# 14. Background Jobs & Scheduling

## Purpose

Four different mechanisms run work outside a request. Explain what each is for, and why the choice is not arbitrary.

## The four mechanisms

| Mechanism | Used for | Where |
|---|---|---|
| Hangfire recurring jobs | cron-scheduled work, visible in a dashboard | Sales, Dashboard.Bff |
| `BackgroundService` | continuous loops tied to the host lifetime | both APIs, the worker |
| Kafka consumers | reacting to another service | both APIs, the worker |
| Startup tasks | one-time preparation before serving traffic | all hosts |

## Hangfire (Sales)

Storage is the `hangfire` PostgreSQL database, so schedules and history survive restarts. Three queues:

```csharp
options.Queues = [HangfireQueueNames.Critical, HangfireQueueNames.Default, HangfireQueueNames.Maintenance];
```

Two jobs today:

| Job id | Class | Cron | Queue | Work |
|---|---|---|---|---|
| `sales-cleanup` | `MaintenanceCleanupJob` | `0 0 * * *` | `maintenance` | delete processed inbox/outbox rows older than 14 days |
| `orders:cancel-expired` | `CancelExpiredPendingOrdersJob` | `*/5 * * * *` | `critical` | cancel orders idle past `ExpirationMinutes` |

### Ids in code, schedules in config

```csharp
public static class SalesRecurringJobIds
{
    public const string MaintenanceCleanup = "sales-cleanup";
    public const string CancelExpiredPendingOrders = "orders:cancel-expired";
}
```

`RecurringJobSettings` carries `Enabled`, `Cron`, and `Queue` — but deliberately **not** the job id:

> Job identifiers are deliberately absent: they stay in service-owned constants so a configuration change cannot create a second recurring job.

Change a configurable id and Hangfire happily registers a *second* job on the old schedule. Both fire. That is a genuinely nasty production incident, prevented by a design decision.

### Registration mechanics are shared

```csharp
public static void ScheduleRecurringJob<TJob>(this IRecurringJobManager manager,
    string recurringJobId, RecurringJobSettings settings, Expression<Func<TJob, Task>> jobExpression)
{
    if (!settings.Enabled) { manager.RemoveIfExists(recurringJobId); return; }
    ArgumentException.ThrowIfNullOrWhiteSpace(settings.Queue);
    ArgumentException.ThrowIfNullOrWhiteSpace(settings.Cron);
    manager.AddOrUpdate(recurringJobId, settings.Queue, jobExpression, settings.Cron,
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}
```

Two details worth stealing:

- **disabling removes the job** rather than skipping it. Skipping would leave it registered on its old schedule, so re-enabling later resurrects a stale cron.
- **UTC always.** A cron in local time silently shifts twice a year.

### Configuration is validated at startup

```csharp
services.AddOptions<SalesRecurringJobsOptions>()
    .Bind(configuration.GetSection(SalesRecurringJobsOptions.SectionName))
    .ValidateOnStart();
```

`SalesRecurringJobsOptionsValidator` names the offending job in its message. `RecurringJobSettings.IsValid()` parses the cron with Cronos (5- or 6-field), and `CancelExpiredPendingOrdersJobOptions.IsValid()` only checks business parameters when the job is enabled — a disabled job needs no schedule.

A typo'd cron fails the deploy, not the 3 a.m. run.

### The job is an adapter

```csharp
public async Task ExecuteAsync(int expirationMinutes, int batchSize, CancellationToken ct = default)
{
    var result = await sender.Send(new CancelExpiredPendingOrders(clock.UtcNow, expirationMinutes, batchSize), ct);
    SalesMetrics.RecordExpiredOrderCancellation(result.ScannedOrderCount, result.CancelledOrderCount,
        result.SkippedOrderCount, result.FailedOrderCount, stopwatch.Elapsed.TotalMilliseconds);
    logger.LogInformation("CancelExpiredPendingOrdersJob completed {ScannedOrderCount} …");
}
```

The job holds no business logic — it dispatches a command, records metrics, logs. The business rule lives in a handler and is unit-testable without Hangfire, and `IClock` means the time is injectable.

## Hangfire (Dashboard.Bff)

Dashboard.Bff has one recurring job:

| Job id | Class | Default schedule | Queue | Work |
|---|---|---|---|---|
| `dashboard:snapshot-refresh` | `DashboardSnapshotRefreshJob` | `* * * * *` | `default` | build and cache `DashboardSnapshot` |

The job is deliberately thin: it calls `IDashboardSnapshotBuilder.BuildAsync`, stores the result through `IDashboardSnapshotCache`, and logs the boundary. It does not contain aggregation logic. The builder is the only component that calls downstream Sales/Inventory HTTP clients.

### Per-order isolation

`CancelExpiredPendingOrdersHandler` processes each order in **its own scope**:

```csharp
using var scope = serviceScopeFactory.CreateScope();
var scopedOrderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
```

One bad order cannot fail the batch; a failure increments `failedOrderCount`, logs a warning, and the loop continues. It also re-checks `CancelDueToExpiration` per order, because the scan result may be stale by the time it is processed.

## `BackgroundService`

| Service | Cadence |
|---|---|
| `SalesOutboxPublisher` / `InventoryOutboxPublisher` | signal-driven, polling fallback |
| `SalesInboxRedriveService` / `InventoryInboxRedriveService` | fixed 15 s |
| `InventoryMaintenanceWorker` | `PeriodicTimer`, once at startup then daily |
| `MongoStartupService`, `KafkaBusService` | one-shot / lifecycle |

The shape is always the same:

```csharp
while (!stoppingToken.IsCancellationRequested)
{
    try { await using var scope = scopes.CreateAsyncScope(); /* work */ }
    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
    { logger.LogError(ex, "… cycle failed"); }
    await signal.WaitAsync(pollInterval, stoppingToken);
}
```

Fresh scope per cycle (a `BackgroundService` is a singleton; `DbContext` is scoped and not thread-safe), catch and continue (one bad cycle must not kill the loop), and always honour the stopping token.

### Signal beats polling

```csharp
public void Notify() => channel.Writer.TryWrite(true);

public async Task WaitAsync(TimeSpan fallbackInterval, CancellationToken ct)
{
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeout.CancelAfter(fallbackInterval);
    try { if (!await channel.Reader.WaitToReadAsync(timeout.Token)) return;
          while (channel.Reader.TryRead(out _)) { } }
    catch (OperationCanceledException) when (!ct.IsCancellationRequested) { /* poll fallback */ }
}
```

A bounded channel of size 1 with `DropWrite`: any number of saves collapses into one wake-up. Publish latency drops from "up to the poll interval" to "immediately", while polling remains as the recovery path — an in-process signal is lost on restart, and another instance's writes never signal you.

### Why Inventory does not use Hangfire

`InventoryMaintenanceWorker` uses a `PeriodicTimer` instead. Inventory has no Hangfire dependency, and adding a whole job framework for one daily cleanup would be disproportionate. It coordinates with a Postgres advisory lock rather than Redis, keeping the dependency list short.

## Startup tasks

```csharp
public static async Task RunStartupTasksAsync(this WebApplication app)
{
    var kafkaBus = await KafkaBusLifecycle.StartAsync(app.Services);
    app.Lifetime.ApplicationStopping.Register(() => KafkaBusLifecycle.StopAsync(kafkaBus).GetAwaiter().GetResult());
    await app.Services.SeedIdentityAsync();
    app.Services.RegisterSalesRecurringJobs();
}
```

The `GetAwaiter().GetResult()` is the one sanctioned blocking call in the solution — `ApplicationStopping` takes a synchronous callback.

`MongoStartupService` shows the right shape for a dependency that may not be up yet: ping with retry (20 attempts, 2 s apart), create indexes idempotently, and rethrow on the final attempt so the container fails fast rather than serving broken.

## Idempotency is mandatory

Every scheduled or looping operation must be safe to run twice, because locks expire and instances restart:

| Operation | Protection | Why it is still safe alone |
|---|---|---|
| Outbox publish | 30 s lease | duplicate publish is deduplicated by the consumer's inbox |
| Sales cleanup | Redis lock | deletes by predicate — running twice deletes nothing extra |
| Inventory cleanup | advisory lock | same |
| Expired-order cancel | per-order re-check | `CancelDueToExpiration` returns false for an already-cancelled order |

The lock is an optimisation. The operation is the guarantee.

## Common mistakes

| Mistake | Consequence |
|---|---|
| Injecting a scoped service into a `BackgroundService` constructor | captive dependency, `DbContext` shared across threads |
| Making the job id configurable | a config change creates a duplicate job |
| Cron in local time | the schedule shifts twice a year |
| Disabling a job by skipping instead of removing | it resurrects on the old schedule |
| No try/catch in the loop | one bad cycle kills the loop until restart |
| Business logic in the job class | untestable without Hangfire |
| Relying on a lock for correctness | locks expire mid-operation |

## Related

- [../tech/background-jobs.md](../tech/background-jobs.md)
- [07-domain-events-and-outbox.md](07-domain-events-and-outbox.md)
- [../tech/retry-and-dead-letter.md](../tech/retry-and-dead-letter.md)
