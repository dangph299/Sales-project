# Frontend Naming Rules

## Files

| Kind | Pattern | Example |
|---|---|---|
| Component | `<name>.component.ts` in `<name>/` | `product-list-page/product-list-page.component.ts` |
| Service | `<name>.service.ts` | `product-api.service.ts` |
| Routes | `<feature>.routes.ts` | `products.routes.ts` |
| Request contract | `<action>-<entity>.request.ts` | `create-product.request.ts` |
| Response contract | `<entity>.response.ts` | `product.response.ts` |
| View/form model | `<topic>.model.ts` | `product-form.model.ts` |
| Mapper | `<topic>.mapper.ts` | `category-tree.mapper.ts` |
| Constants | `<topic>.ts` | `product-status.ts` |
| Pipe | `<name>.pipe.ts` | `money.pipe.ts` |
| Utility | `<verb>-<noun>.ts` | `describe-api-error.ts` |
| Test | `<file>.spec.ts` next to the source | `cart-line.model.spec.ts` |

All file and folder names are `kebab-case`.

## Symbols

| Kind | Pattern | Example |
|---|---|---|
| Component class | `<Name>Component` | `ProductListPageComponent` |
| Service class | `<Name>Service` / `<Name>ApiService` / `<Name>Store` | `SessionService`, `ProductApiService`, `CommonStore` |
| Interface | PascalCase, no `I` prefix | `ProductResponse`, `PagedResult<T>` |
| Type union | PascalCase | `ProductStatus`, `RealtimeConnectionState` |
| Constant object | PascalCase for code maps, camelCase for display maps | `CategoryCodes`, `productStatusDisplays` |
| Function | camelCase verb-first | `emptyProductForm`, `describeApiError` |
| Signal | camelCase noun | `loading`, `errorMessage`, `rows` |
| Selector | `app-<kebab-name>` | `app-product-list-page` |
| Route export | `<feature>Routes` | `productsRoutes` |
| Output | past-tense camelCase | `saved`, `cancelled`, `signedOut` |

## Backend-mirrored values

- Status unions use the **exact** backend string values (`'PendingInventory'`, not `'pending-inventory'`).
- Response interface fields use the exact camelCase JSON field names from the API.
- Realtime event names match the server constant exactly (`'OrderStatusChanged'`), declared once in a feature service (`orderRealtimeEvents`).
- Hub method names match the server hub methods (`'SubscribeToOrder'`).

## Related

- [folder-structure.md](folder-structure.md)
- [coding-rule.md](coding-rule.md)
