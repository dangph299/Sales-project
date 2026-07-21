# Controller Rules

## DO

- One controller per resource, `sealed`, `[ApiController]`, `[Route("api/<resource>")]`.
- Authorize at class level; tighten per action with `[Authorize(Roles = "...")]`.
- Inject `ISender` only.
- Use explicit constructor injection with `private readonly` fields — no primary constructors for multi-dependency controllers.
- Return `IActionResult` built from `this.ToOkResponse(...)`, `this.ToCreatedResponse(uri, ...)`, `this.ToNoContentResponse()`.
- Call `Response.SetEtag(dto)` after loading or mutating a versioned resource.
- Call `Request.RequireVersion()` on mutating endpoints that need `If-Match`.
- Accept `CancellationToken ct` last and pass it to `_sender.Send(...)`.
- Write XML docs on every action: summary, each parameter, and the returned status codes.

## DON'T

- No `DbContext`, repository, read service, or domain type in a controller.
- No `try`/`catch` — `ApiExceptionHandler` owns error translation.
- No manual `ApiErrorResponse` construction.
- No business branching (status checks, price math, validation). Push it into the command/handler/aggregate.
- No `async void`, no `.Result`.
- No new Minimal API endpoints for business APIs.

## Template

```csharp
[ApiController]
[Authorize(Roles = "Admin,Sales")]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender) => _sender = sender;

    /// <summary>Requests confirmation of a draft order.</summary>
    /// <param name="id">Order to confirm, from the route.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the updated order and a new <c>ETag</c>.</returns>
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var order = await _sender.Send(new ConfirmOrder(id, Request.RequireVersion()), ct);
        Response.SetEtag(order);
        return this.ToOkResponse(order);
    }
}
```

## Exceptions to the rule

`AuthController` uses `UserManager<ApplicationUser>` and `SalesDbContext` directly and is `[AllowAnonymous]`. Authentication is not a Sales business use case and deliberately bypasses CQRS. Do not copy this pattern for business endpoints.

## Related

- [api-guideline.md](api-guideline.md)
- [security-rule.md](security-rule.md)
