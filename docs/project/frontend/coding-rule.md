# Frontend Coding Rules

## TypeScript

- `strict` mode is on, together with `noImplicitOverride`, `noPropertyAccessFromIndexSignature`, `noImplicitReturns`, `noFallthroughCasesInSwitch`. Never relax them.
- No `any`. Use `unknown` plus a narrowing guard.
- No non-null assertion (`!`) except on `@Input({ required: true })` fields.
- Prefer `interface` for data shapes, `type` for unions and function types.
- Status values are string unions of the exact backend codes, never TS `enum`:
  ```ts
  export type ProductStatus = 'Draft' | 'Published' | 'Discontinued';
  ```
- Use `readonly` on class members that are never reassigned.
- 2-space indent, single quotes, semicolons.

## Angular

- `inject()` for dependencies, not constructor parameters.
- `readonly` signals for state; assign with `.set()`/`.update()`.
- `standalone: true` and an explicit `imports` array on every component.
- Templates in `.html`, styles in `.scss`, except for components small enough to be a one-line inline template (`StatusTagComponent`, `AppComponent`).
- `async`/`await` with promises from `ApiClientService`. Use `firstValueFrom` only inside `ApiClientService`.
- Clean up subscriptions and realtime handlers with the unsubscribe function they return, in `ngOnDestroy`.

## Errors

- Catch failures where you can show them; convert with `describeApiError(error)` and put the message into an error signal.
- Never `console.error` as the only handling of a user-visible failure.
- Never swallow an error silently.

## Forbidden

- `HttpClient` outside `ApiClientService`.
- Hardcoded backend URLs, ports, or seeded GUIDs.
- Business logic in a template.
- Direct DOM manipulation.
- `any`, `@ts-ignore`, disabling lint rules inline.
- Importing from another feature (except `features/common`).

## Related

- [component-rule.md](component-rule.md)
- [naming-rule.md](naming-rule.md)
- [checklist.md](checklist.md)
