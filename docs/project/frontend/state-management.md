# State Management Rules

## Signals only

- Angular signals for all state. No NgRx, no Redux, no `BehaviorSubject`-as-store.
- `signal<T>(initial)` for writable state, `computed()` for derived values, `effect()` only for genuine side effects.
- Expose state as `readonly` fields; mutate with `.set()` / `.update()` inside the owning class.
- Never mutate an array or object in place; replace it.

## Where state lives

| Scope | Home |
|---|---|
| One page | the routed page component |
| One feature across pages | `@Injectable({ providedIn: 'root' })` service in `features/<name>/services/` |
| Whole app | `core/` service |

Root services today: `SessionService` (tokens), `ApiEndpointConfigurationService` (base URLs), `SignalrConnectionService` (connection state), `CommonStore` (reference data), `OrderRealtimeService` (order subscriptions), `BreadcrumbService` (shell).

## Standard page state

Every data-loading page exposes at least:

```ts
readonly loading = signal(false);
readonly errorMessage = signal('');
```

Set `loading` true before the call, clear the error, set the error in `catch`, clear `loading` in `finally`.

## Persistence

- Only `SessionService` and `ApiEndpointConfigurationService` touch `localStorage`, and only for tokens and base URLs.
- No other service reads or writes `localStorage` directly.
- Initialize a signal from storage in the field initializer; write through on every mutation.

## Caching reference data

- `CommonStore` loads colors, sizes, and categories once for the application lifetime and shares one in-flight promise between concurrent callers.
- A failed load clears the in-flight promise so `ensureLoaded()` retries; `reload()` forces a refetch.
- Components call `ensureLoaded()`; they never fetch reference data themselves.
- Match reference data on `code` (`CategoryCodes.Uncategorized`, `SizeCodes.Medium`) and submit the `id` that comes with it. **Never hardcode a backend GUID.**

## Realtime state

- `SignalrConnectionService` owns the connection lifecycle and exposes `state`. It knows nothing about hubs or groups.
- Feature services (`OrderRealtimeService`) own hub URL, groups, and event names, and register a resubscribe callback that replays group membership after a reconnect.
- Components subscribe via the feature service and call the returned unsubscribe function in `ngOnDestroy`.
- Realtime is a hint to refresh, not a replacement for the REST read. Re-fetch on notification.

## Related

- [architecture.md](architecture.md)
- [api-rule.md](api-rule.md)
