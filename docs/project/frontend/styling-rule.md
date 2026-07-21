# Styling Rules

## Toolkit

- ng-zorro-antd (`ng-zorro-antd@18`) is the component library. Use its components before writing custom markup.
- Icons are registered explicitly in `app.config.ts` via `provideNzIcons([...])`. Add the icon there before using it; an unregistered icon renders nothing.
- Locale is `en_US` through `provideNzI18n`.
- Animations are enabled globally with `provideAnimations()`.

## SCSS

- One `.scss` file per component, referenced with `styleUrl`. Styles are component-scoped by Angular's default emulated encapsulation.
- Global styles live only in `src/styles.scss`.
- Never use `::ng-deep` in a new component. If an ng-zorro internal must be restyled, do it in `styles.scss` with a narrow selector and a comment explaining why.
- No inline `style="..."` in templates except for a genuinely dynamic value bound with `[style.x]`.

## Layout

- Page structure uses `nz-layout`, `nz-card`, `nz-table`, `nz-form` rather than hand-rolled flex containers.
- The application shell (header / sidebar / status bar / breadcrumbs) is owned by `layout/`. A feature never renders chrome.
- Responsive behaviour comes from ng-zorro grid props (`nzXs`, `nzMd`) — avoid custom media queries unless the grid cannot express it.

## Status and formatting

- Status colour is never chosen in a template. Declare a `Record<Status, StatusDisplay>` in `features/<name>/constants/` with `label` + `tone`, and render with `<app-status-tag [display]="...">`.
- Tones are `success | warning | danger | info | neutral`; `StatusTagComponent` maps them to nz-tag colours. Do not pass an nz colour directly.
- Money renders through the `money` pipe, dates through `dateTime`, price ranges through `priceRange`, truncation through `compactText`.
- Loading and error states render through `PageStateComponent`, not ad-hoc spinners.

## Accessibility

- Buttons are `<button nz-button>`, links are `<a routerLink>`. Never a clickable `<div>`.
- Every icon-only control needs a `nzTooltipTitle` or `aria-label`.
- Every form control has a `nz-form-label` bound to it.

## Related

- [component-rule.md](component-rule.md)
