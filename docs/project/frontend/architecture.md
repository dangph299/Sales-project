# Frontend Architecture Rules

Angular 18 standalone application at `src/Web/Sales.Web`. UI kit: ng-zorro-antd. Realtime: `@microsoft/signalr`.

## Layers

```
core/      cross-cutting singletons: http transport, session, config, realtime, navigation
layout/    application shell: header, sidebar, status bar, breadcrumbs
shared/    presentation-only components, pipes, utilities, display models
features/  one folder per business area, feature-first and self-contained
```

Dependency direction: `features -> shared -> core`. Never the reverse.

## Rules

- Standalone components only. No `NgModule`.
- Bootstrap through `bootstrapApplication(AppComponent, appConfig)`; all global providers live in `app.config.ts`.
- Every feature is lazy-loaded from `app.routes.ts` via `loadChildren` pointing at `features/<name>/<name>.routes.ts`.
- A feature never imports from another feature, except from `features/common`, which owns shared backend lookup data.
- `core/` never imports from `features/`.
- `shared/` is presentation-only: no HTTP, no business vocabulary, no feature imports.
- Path aliases `@core/*`, `@shared/*`, `@layout/*`, `@features/*` exist in `tsconfig.json`. Prefer them over deep relative paths in new code.

## Feature folder

```
features/<name>/
  <name>.routes.ts
  api/                 <Name>ApiService + api/requests + api/responses (wire types)
  models/              view models and form models
  mappers/             response -> view model translation (only when non-trivial)
  constants/           status unions and display maps
  components/          feature-scoped presentational components
  pages/               routed container components
  services/            feature state/realtime services
```

- `api/requests` and `api/responses` mirror the backend wire shape exactly; do not reshape there.
- `models/` holds what the UI actually renders. Convert between the two in `mappers/`.

## State

- Angular signals only. No NgRx, no `BehaviorSubject` for component state.
- Page-level state lives in the routed page component.
- Cross-page state lives in a `providedIn: 'root'` service exposing `signal`/`computed` (`SessionService`, `CommonStore`, `ApiEndpointConfigurationService`, `SignalrConnectionService`).
- Derived values are `computed`, never recalculated in the template.

## Transport

- `ApiClientService` is the only place that touches `HttpClient`.
- Feature API services call `ApiClientService` and return typed promises.
- Components never call `HttpClient` and never build URLs.
- Backend base URLs come from `ApiEndpointConfigurationService` (`/sales-api`, `/inventory-api`), proxied in dev by `proxy.conf.json`.

## Related

- [component-rule.md](component-rule.md)
- [state-management.md](state-management.md)
- [api-rule.md](api-rule.md)
- Learning: [../../guides/16-frontend-architecture.md](../../guides/16-frontend-architecture.md)
