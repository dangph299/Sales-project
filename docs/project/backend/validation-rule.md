# Validation Rules

## Three layers, three responsibilities

| Layer | Enforces | Mechanism | Failure |
|---|---|---|---|
| API model binding | request is well-formed JSON/route/query | ASP.NET Core model binding | `400` with `validation` error code |
| Application | request shape is plausible before touching the domain | FluentValidation via `ValidationBehavior` | `400` with `validation` + field errors |
| Domain | business invariants that must always hold | `DomainException` from the aggregate | `400` with `invalid_operation` |

Never move a domain invariant into a validator. A validator improves the error message; the aggregate is the guarantee.

## FluentValidation

- One `sealed class <Command>Validator : AbstractValidator<TCommand>` per command, in `Features/<Aggregate>/Validators/`.
- Registered by assembly scan (`AddValidatorsFromAssembly`). Never register by hand.
- Queries are not validated; paging is clamped by `Paging.Normalize`.
- Reusable rules become extension methods:
  - cross-feature → `Common/Extensions/CommonValidationRules.cs` (`ValidAggregateId()`, `ValidExpectedVersion()`)
  - feature-scoped → `Features/<Aggregate>/Validators/<Aggregate>ValidationRules.cs` (`ValidPhone()`, `ValidCustomerName()`, `HaveUniqueProducts()`)
- Nested collections use `RuleForEach(...).SetValidator(new XValidator())` or `.ChildRules(...)`.
- Give a custom `.WithMessage(...)` whenever the default text would not help a client.

## Standard rules

- Aggregate ids: `.ValidAggregateId()` (non-empty GUID).
- Expected version: `.ValidExpectedVersion()` (≥ 0).
- Name: `.NotEmpty().MaximumLength(200)`.
- Description: `.MaximumLength(1000)`.
- Status string: `.NotEmpty().MaximumLength(32)`.
- Price / quantity: `.GreaterThanOrEqualTo(0)` / `.GreaterThan(0)`.
- Discount: `.NotNull().InclusiveBetween(0, 100)`.
- Phone: `.ValidPhone()` (9–15 digits after stripping non-digits).
- Collections: `.NotEmpty()` plus a uniqueness rule where duplicates are illegal.
- Max length in a validator must match `HasMaxLength` in the EF configuration.

## Status strings

Commands accept status as `string` and parse with `Enum.TryParse<T>(status, ignoreCase: true, out var value)`; an unparsable value throws `DomainException("... status is invalid.")`. The validator only checks presence and length. Keep both.

## Related

- [exception-rule.md](exception-rule.md)
- [cqrs-rule.md](cqrs-rule.md)
- Reference: [../../tech/business/validation-rules.md](../../tech/business/validation-rules.md)
