# Frontend Feature Checklist

Every generated frontend feature must satisfy every applicable item.

## Architecture

- [ ] Feature lives in its own `features/<name>/` folder with the standard sub-folders.
- [ ] Route file `<name>.routes.ts` exports `<name>Routes` and is lazy-loaded from `app.routes.ts`.
- [ ] No import from another feature (except `features/common`).
- [ ] Nothing feature-specific was added to `core/` or `shared/`.
- [ ] `shared/` additions are presentation-only and business-vocabulary-free.

## Components

- [ ] Every component is `standalone: true` with an explicit `imports` array.
- [ ] Selector is prefixed `app-`.
- [ ] Container vs. presentational split is respected; presentational components inject no API service or store.
- [ ] Dependencies use `inject()` into `private readonly` fields.
- [ ] `@Input({ required: true })` for inputs the component cannot render without.
- [ ] `@Output()` names are past-tense events.
- [ ] Template and styles are separate files (unless the template is a single line).
- [ ] Subscriptions and realtime handlers are unsubscribed in `ngOnDestroy`.

## State

- [ ] All state is signals; no `BehaviorSubject`, no NgRx.
- [ ] Derived values use `computed()`.
- [ ] Data-loading pages expose `loading` and `errorMessage` signals and render them through `PageStateComponent`.
- [ ] Cross-page state lives in a root-provided service, not in a component.
- [ ] `localStorage` is touched only by `SessionService` / `ApiEndpointConfigurationService`.
- [ ] Reference data is read through `CommonStore.ensureLoaded()`, never fetched directly.

## API

- [ ] All HTTP goes through a `<Feature>ApiService` → `ApiClientService`. No `HttpClient` in a component.
- [ ] Base URL comes from `ApiEndpointConfigurationService`; no hardcoded host or port.
- [ ] Request/response interfaces mirror the backend wire shape exactly, one per file.
- [ ] Paged reads use `getPage` and the shared `PagedResult<T>`.
- [ ] Reads that need concurrency use `getWithEtag`; writes use `putWithEtag`/`postWithEtag` with that ETag.
- [ ] `409` is surfaced to the user as "reload and retry", never auto-retried with a stale ETag.
- [ ] Errors are converted with `describeApiError`; field errors read from `result.validationErrors`.
- [ ] No `Authorization` header is set outside `ApiClientService`.

## Contracts

- [ ] Status types are string unions of exact backend codes, not TS enums.
- [ ] No seeded backend GUID appears anywhere; ids come from lookup responses.
- [ ] Realtime event and hub method names match the server constants exactly.

## Styling

- [ ] ng-zorro components used before custom markup.
- [ ] New icons registered in `app.config.ts`.
- [ ] No `::ng-deep` in a new component.
- [ ] Status rendering goes through `constants/` display maps + `StatusTagComponent`.
- [ ] Money/date/text formatting uses the shared pipes.
- [ ] Icon-only controls have a tooltip or `aria-label`; form controls have labels.

## Naming

- [ ] Files and folders are `kebab-case` and follow the file-name patterns.
- [ ] Classes, interfaces, signals, and route exports follow the symbol patterns.

## TypeScript

- [ ] No `any`, no `@ts-ignore`, no non-null assertion outside required inputs.
- [ ] `npm run build` passes under `strict`.

## Testing

- [ ] Mappers, formatters, and model helpers have unit tests.
- [ ] The routed page has at least a smoke test.
- [ ] The new route is covered by `app-routes-smoke.spec.ts`.
- [ ] Fakes are used for API services; no real network or SignalR in tests.
- [ ] `npm test` passes.

## Documentation

- [ ] `docs/tech/frontend-map.md` updated when a feature, route, or core service was added.
- [ ] `docs/project/frontend/` updated when a rule changed.
