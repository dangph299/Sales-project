# Async Rules

## Signatures

- Async methods return `Task` / `Task<T>` and end in `Async`. `ValueTask` only when overriding a BCL/EF interceptor signature.
- `CancellationToken` is the last parameter, named `cancellationToken` (or `ct` in API/read-service code that already uses it).
- Give it a default only on ports/interfaces (`CancellationToken cancellationToken = default`), not on handlers.
- Never `async void`. Background loops use `protected override async Task ExecuteAsync(CancellationToken)`.

## Propagation

- Pass the token to every awaited call: EF, HTTP, Kafka, Redis, `Task.Delay`.
- Kafka message processing calls `CancellationToken.None` deliberately — the message must finish or be recorded as failed. Do not "fix" that.
- `await` directly instead of returning `Task` when a `using`/`await using` scope must stay alive.
- Return the `Task` without `await` in thin pass-through delegations (`InventoryReadService`, `CachedProductReadService.GetForWriteResultAsync`).

## Forbidden

- `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`.
  - Single documented exception: the `ApplicationStopping` shutdown hook in `StartupTaskExtensions`, where the callback is synchronous.
- `Task.Run` to offload work in a request path.
- `Thread.Sleep`. Use `await Task.Delay(..., cancellationToken)`.
- `ConfigureAwait(false)` — ASP.NET Core has no synchronization context; adding it is noise.

## Concurrency

- `DbContext` is not thread-safe. Never run two awaits on the same context concurrently.
- `Task.WhenAll` is allowed only over independent work that does not share a `DbContext` — e.g. running FluentValidation validators in `ValidationBehavior`.
- Background services create a fresh scope per cycle: `await using var scope = scopes.CreateAsyncScope();`.
- Long-running loops must exit on cancellation and swallow only `OperationCanceledException` caused by shutdown.

```csharp
try { await Task.Delay(pollInterval, stoppingToken); }
catch (OperationCanceledException) { break; }
```

## Sequential DB access

- Loop with `await` inside; do not parallelise EF calls.
- Prefer one bulk query (`Where(x => ids.Contains(x.Id))`) over N sequential lookups.

## Related

- [performance-rule.md](performance-rule.md)
- [dependency-rule.md](dependency-rule.md)
