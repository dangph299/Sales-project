# DTO Rules

## General

- All DTOs are `sealed record`.
- DTOs live in `<Service>.Application/Features/<Aggregate>/DTOs/`.
- Never expose an EF entity, aggregate, or domain value object across an API or cache boundary.
- Never put behavior on a DTO beyond trivial computed properties.
- Never reference a DTO from Domain.

## Positional vs. init properties

- Prefer a positional record when the shape is stable (`OrderDto`, `OrderLineDto`, `InventorySnapshot`).
- Add optional fields as `{ get; init; }` on the record body when appending to an existing positional record would break call sites (`ProductDto.ProductCode`, `.Variants`, `.Category`).
- Optional trailing positional parameters with defaults are used in `CustomerDto` for the same reason. Match the file you are editing.

## Content rules

- Enums are exposed as `string` (`Status`), converted with `.ToString()` in the Mapster register or projection. Never leak the numeric value.
- `Money` is exposed as `decimal` (`source.UnitPrice.Amount`).
- Every versioned resource DTO carries `Version` and `UpdatedAt`; the API turns `Version` into the `ETag`.
- Soft-delete DTOs carry `IsDelete`, `DeleteByUser`, `DeletedAt`.
- Paged reads return `PagedResult<T>(Items, Page, PageSize, Total)` — never a bare array plus headers.

## Request/response models

- HTTP request bodies are `sealed class` with `{ get; init; }` in `<Service>.Api/Models/Requests/`. Give string properties a `= string.Empty` default.
- Never include an `Id` in a request body when it already comes from the route.
- Never include a backend-assigned business code (`ProductCode`, `CategoryCode`, `CustomerCode`) in a create request — those come from `I*CodeGenerator`.
- HTTP response models live in `<Service>.Api/Models/Responses/` and are only used where an Application DTO would be wrong (`TokenResponse`).
- Binding an Application command directly from `[FromBody]` is allowed when the command already is the wire shape (`CreateOrder`, `CreateProductCommand`).

## Mapping

- Mapster only. One `IRegister` per aggregate, in `Features/<Aggregate>/Mapping/`.
- Configure conversions there, not in the handler: `Map(dest => dest.Status, src => src.Status.ToString())`.
- Read services that need joins or aggregation build the DTO explicitly instead of mapping (`ProductReadService.MapProduct`). That is intended — do not force it through Mapster.
- `MappingTests` in the test projects assert every register compiles. Add a case when adding a register.

## Related

- [entity-rule.md](entity-rule.md)
- [serialization-rule.md](serialization-rule.md)
