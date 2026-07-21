# Component Rules

## Two kinds of component

| Kind | Location | Responsibility |
|---|---|---|
| Page (container) | `features/<name>/pages/<page>/` | routing target; owns state, calls API services, handles errors |
| Presentational | `features/<name>/components/` or `shared/components/` | renders `@Input()`, emits `@Output()`; no data access |

A presentational component never injects an API service, `SessionService`, or a store.

## Rules

- `standalone: true`, explicit `imports`, selector prefixed `app-`.
- Inject with `inject()`; hold dependencies in `private readonly` fields.
- Public state is `readonly` signals; templates read `state()`.
- `@Input({ required: true })` for inputs the component cannot render without.
- `@Output()` names are past-tense events (`saved`, `cancelled`, `signedOut`).
- One component per file; template and styles in sibling `.html`/`.scss` files.
- Do not put a status→label/colour map in a component. It belongs in `features/<name>/constants/` and renders through `StatusTagComponent`.
- Formatting goes through the shared pipes (`money`, `dateTime`, `compactText`, `priceRange`), not string building in the class.

## Page component template

```ts
@Component({
  selector: 'app-product-list-page',
  standalone: true,
  imports: [CommonModule, /* nz modules */, ProductFormComponent],
  templateUrl: './product-list-page.component.html',
  styleUrl: './product-list-page.component.scss'
})
export class ProductListPageComponent implements OnInit {
  private readonly productApi = inject(ProductApiService);

  readonly rows = signal<ProductResponse[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal('');

  async ngOnInit(): Promise<void> { await this.reload(); }

  private async reload(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const page = await this.productApi.search({ page: 1, pageSize: 20 });
      this.rows.set(page.items);
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.loading.set(false);
    }
  }
}
```

Every data-loading page must expose `loading` and `errorMessage` signals and render them through `PageStateComponent`.

## Forms

- Reactive forms (`ReactiveFormsModule`, provided globally in `app.config.ts`).
- The form's shape is a `models/<name>-form.model.ts` interface with an `empty<Name>Form(...)` factory.
- Defaults that depend on backend data (default category, default size) are passed in by the caller from `CommonStore` — never hardcoded.
- Validate in the form; the backend is still the authority. Show server validation errors from `ApiClientError.result.validationErrors`.

## Destructive actions

- Confirm through `shared/utilities/confirm-action.ts`. Never call `window.confirm` directly in a component.

## Related

- [styling-rule.md](styling-rule.md)
- [state-management.md](state-management.md)
