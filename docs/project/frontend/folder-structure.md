# Frontend Folder Structure Rules

```
src/Web/Sales.Web/
  angular.json  tsconfig*.json  package.json  proxy.conf.json
  src/
    index.html  main.ts  styles.scss  favicon.svg
    app/
      app.component.ts  app.config.ts  app.routes.ts
      core/
        api/         ApiClientService, ApiResponseReader, envelope + error + paged models
        auth/        SessionService, AuthApiService, TokenResponse
        config/      ApiEndpointConfigurationService
        health/      HealthApiService
        navigation/  navigation config + item model
        realtime/    SignalrConnectionService, RealtimeConnectionState
      layout/        app-header, app-layout, app-sidebar, app-status-bar, breadcrumb
      shared/
        components/  presentation-only (page-state, status-tag)
        models/      StatusDisplay / StatusTone
        pipes/       money, date-time, compact-text, price-range
        utilities/   confirm-action, describe-api-error, display-formatters
      features/
        categories/ common/ customers/ dashboard/ inventory/ orders/ products/
```

## Feature folder

```
features/<name>/
  <name>.routes.ts
  api/
    <name>-api.service.ts
    requests/<action>-<entity>.request.ts
    responses/<entity>.response.ts
  components/<component-name>/<component-name>.component.{ts,html,scss,spec.ts}
  constants/<topic>.ts
  mappers/<topic>.mapper.ts
  models/<topic>.model.ts
  pages/<page-name>/<page-name>.component.{ts,html,scss,spec.ts}
  services/<name>.service.ts
```

## Rules

- Every folder segment and file name is `kebab-case`.
- A component owns its own folder containing `.ts`, `.html`, `.scss`, and its `.spec.ts`.
- One route file per feature, exported as `<name>Routes`, wired into `app.routes.ts` with `loadChildren`.
- Add nothing to `core/` that is specific to one feature.
- Add nothing to `shared/` that knows a business term.
- `features/common` is the only feature other features may import from; it owns backend reference data.
- Do not create a folder until it has a file in it.

## Related

- [architecture.md](architecture.md)
- [naming-rule.md](naming-rule.md)
