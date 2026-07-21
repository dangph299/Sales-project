# Frontend Testing Rules

## Stack

- Jasmine + Karma via `ng test` (`@angular-devkit/build-angular:karma`).
- Specs live next to the file under test as `<file>.spec.ts`.
- Playwright specs for cross-service flows live in `tests/Playwright/specs`, not in the Angular project.

## What must have a test

| Subject | Required |
|---|---|
| Pure mapper / formatter / model helper | yes — this is the highest-value layer |
| Transport parsing (`ApiResponseReader`) | yes, per status-code branch |
| Root store with non-trivial logic (`CommonStore`) | yes |
| Routed page component | smoke test: renders, loads, shows error state |
| Presentational component with logic (`StatusTagComponent`) | yes |
| Thin API service that only forwards to `ApiClientService` | no |
| Template-only component | no |

`app-routes-smoke.spec.ts` asserts every lazy route resolves. Add your route there when adding a feature.

## Rules

- Test behaviour through the public surface. Never assert on a private field.
- Fake the API service, not `HttpClient`, when testing a component.
- Fake `ApiClientService` when testing a feature API service.
- Use `TestBed.configureTestingModule({ imports: [TheStandaloneComponent], providers: [...] })` — standalone components go in `imports`.
- Provide fakes with `{ provide: X, useValue: fake }`.
- No real network, no real SignalR connection, no `localStorage` assumptions — clear it in `beforeEach` when the subject reads it.
- Deterministic only: no `setTimeout` waits, no reliance on real timing.

## Assertions

- Assert the observable outcome: rendered text, emitted output, signal value, error message.
- For error paths, assert the message produced by `describeApiError`, not the raw exception.
- For status displays, assert the `label` and `tone`, not the nz colour string.

## Commands

```bash
cd src/Web/Sales.Web
npm test                 # watch mode
npm run build            # must pass; strict mode catches most regressions

cd tests/Playwright
npx playwright test      # full cross-service flows against a running stack
npm run test:audit       # end-to-end audit verification
```

## Related

- [checklist.md](checklist.md)
- Backend: [../backend/testing-rule.md](../backend/testing-rule.md)
