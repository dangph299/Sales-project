# Implement Aggregated Swagger UI (Sales + Inventory)

## Objective

Implement a development-only aggregated Swagger UI without introducing any new gateway, reverse proxy, or API service.

The goal is:

* `Sales.Api` continues exposing its own Swagger as today.
* `Inventory.Api` continues exposing its own Swagger as today.
* `http://localhost:5000/swagger` (Sales.Api) becomes a single Swagger UI that contains two documents:

  * **Sales API**
  * **Inventory API**
* The Inventory OpenAPI document is fetched directly by the browser from Inventory.Api through CORS.
* Runtime API routing must remain unchanged.

---

# Architecture

## BuildingBlocks.Web

`BuildingBlocks.Web` owns the reusable Swagger UI implementation.

It must **not**:

* reference Sales
* reference Inventory
* read IConfiguration
* know any application-specific URLs

It should only know how to render one or many Swagger documents.

---

## Sales.Api

Sales.Api is responsible for:

* reading configuration
* deciding which external Swagger documents should appear
* passing them into `UseApiDocumentation(...)`

Sales.Api owns the configuration.

---

## Inventory.Api

Inventory.Api remains completely independent.

It only:

* exposes its own Swagger
* enables Development-only CORS so browsers can fetch its swagger.json and execute "Try it out" requests from the Sales Swagger UI.

Inventory.Api must not reference Sales.Api.

---

# Required Changes

## 1. Create a reusable model

Do NOT use tuples in public APIs.

Create a dedicated model inside BuildingBlocks.Web.

Example:

```csharp
public sealed record SwaggerDocumentEndpoint(
    string DisplayName,
    string Url);
```

Use this model everywhere instead of:

```csharp
(string Name, string Url)
```

---

## 2. Extend UseApiDocumentation()

Current API:

```csharp
app.UseApiDocumentation("Sales API");
```

New API:

```csharp
app.UseApiDocumentation(
    "Sales API",
    additionalDocuments:
    [
        new SwaggerDocumentEndpoint(
            "Inventory API",
            inventorySwaggerUrl)
    ]);
```

Implementation requirements:

Always register the local document:

```csharp
options.SwaggerEndpoint(
    "/swagger/v1/swagger.json",
    applicationName);
```

Then register every additional document:

```csharp
foreach (...)
{
    options.SwaggerEndpoint(
        document.Url,
        document.DisplayName);
}
```

Do not modify AddApiDocumentation().

Only extend UseApiDocumentation().

Backward compatibility must be preserved.

---

## 3. Sales.Api configuration

Add a Development configuration value.

Example:

```json
{
  "Swagger": {
    "InventoryApiUrl": "http://localhost:5001/swagger/v1/swagger.json"
  }
}
```

Sales.Api reads this configuration.

BuildingBlocks.Web must never access IConfiguration.

---

## 4. Inventory CORS

Enable CORS only in Development.

Allowed origins:

```
http://localhost:5000
https://localhost:5002
```

Policy should allow:

* any header
* any method

Do NOT enable AllowCredentials() unless it is already required.

The middleware order must remain correct:

```
UseRouting()

UseCors()

UseAuthentication()

UseAuthorization()
```

---

## 5. Preserve existing behavior

Do NOT modify:

* JWT configuration
* AuthorizeOperationFilter
* SwaggerGen configuration
* API routing
* docker-compose ports
* Inventory standalone Swagger UI

Inventory Swagger must continue working at:

```
http://localhost:5001/swagger
```

Sales Swagger must continue working at:

```
http://localhost:5000/swagger
```

except now it contains an additional document named "Inventory API".

---

# Design Constraints

* No API Gateway
* No Reverse Proxy
* No YARP
* No new service
* No HTTP proxy endpoint inside Sales.Api
* Browser fetches Inventory swagger.json directly

---

# Acceptance Criteria

* Sales Swagger UI contains two selectable documents.
* Inventory Swagger UI still works independently.
* Selecting "Inventory API" loads Inventory's OpenAPI document successfully.
* "Try it out" works against Inventory endpoints through browser CORS.
* No dependency from BuildingBlocks.Web to Sales.Api or Inventory.Api.
* No dependency from Inventory.Api to Sales.Api.
* Public APIs use `SwaggerDocumentEndpoint` instead of tuples.
* Existing Swagger behavior remains unchanged when `additionalDocuments` is omitted.
