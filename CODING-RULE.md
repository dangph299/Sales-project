# Coding Rules

## Program.cs

* `Program.cs` is only the application composition root.
* `Program.cs` must not contain business logic.
* `Program.cs` must not contain long-form infrastructure configuration.
* `Program.cs` should only call clear extension methods for service registration, middleware configuration, and startup tasks.
* Do not declare request-handling lambdas in `Program.cs`.

## API

* Do not use Minimal API for service APIs.
* All HTTP APIs must use ASP.NET Core Controllers.
* Controllers only receive requests, rely on ASP.NET Core validation/model binding, call the Application layer, and return `IActionResult`.
* Controllers must not contain business logic.
* Controllers must not access `DbContext` or repositories directly for business use cases.
* Controllers must communicate with the Application layer.

## Swagger / OpenAPI

* Every API service must configure Swagger/OpenAPI.
* Do not configure Swagger directly in `Program.cs`.
* Swagger must be packaged behind extension methods.
* Controllers and actions must have XML documentation (`<summary>`) so Swagger can display useful descriptions.
* Swagger must support JWT Bearer Authentication and expose the Authorize button.
* Do not duplicate Swagger configuration across microservices; shared pieces belong in `BuildingBlocks`.
* Swagger UI should only be enabled in Development unless a service has an explicit documented convention.

## Service Registration

* Each concern must have its own extension method.
* Do not create god extensions that hide unrelated responsibilities.
* One extension should have one clear responsibility.
* Shared infrastructure registration used by multiple services belongs in `BuildingBlocks`.

## Middleware

* Middleware configuration must be grouped behind extension methods.
* Do not configure middleware directly in `Program.cs`.
* Keep exception handling, request logging, observability, authentication, authorization, and API documentation configuration separated by concern.

## Startup Tasks

* Do not run database migrations, start Kafka, or register application lifetime callbacks directly in `Program.cs`.
* Put startup work behind explicit startup task extensions or hosted services.
* Startup tasks must preserve existing execution behavior and DI lifetimes.

## Shared Infrastructure

* Components reused by multiple services, such as authentication, observability, logging, problem details, and Swagger configuration, belong in `BuildingBlocks`.
* Do not duplicate infrastructure configuration across services.
* Infrastructure projects must not depend on API projects.

## Clean Architecture

* Follow the Dependency Rule.
* API depends on Application and Infrastructure only at the composition root.
* Application must not depend on Infrastructure or API.
* Domain must not depend on Application, Infrastructure, or API.
* Infrastructure must not depend on API.
* Controllers must not bypass the Application layer for business use cases.

## General

* Do not duplicate code.
* Do not use generic static helper classes as a dumping ground.
* Do not create abstractions without a clear need.
* Keep naming consistent across the solution.
* Do not use `#region`.
* Each class must have one clear responsibility.
