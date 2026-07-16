# Repository Guidelines

## Project Structure & Module Organization

This repository is a .NET 10 sales management system organized by bounded context.

- `src/Services/Sales`: Sales API, application, domain, and infrastructure projects.
- `src/Services/Inventory`: Inventory API, application, domain, and infrastructure projects.
- `src/Services/AuditLog`: Audit worker and Mongo-backed audit infrastructure.
- `src/Shared`: reusable building blocks for domain, application, contracts, infrastructure, observability, and web hosting.
- `src/Web/Sales.Web`: Angular web client.
- `tests`: xUnit test projects plus `tests/Playwright` browser-flow tests.
- `docker/docker-compose.yml`: local infrastructure and service image orchestration.

Keep generic infrastructure in `src/Shared/BuildingBlocks.*`; keep business rules inside the owning service.

## Build, Test, and Development Commands

Run commands from the repository root unless noted.

```bash
dotnet restore Sales.sln
dotnet build Sales.sln --no-restore
dotnet test Sales.sln --no-build --no-restore
docker compose -f docker/docker-compose.yml build sales-api inventory-api audit-worker
docker compose -f docker/docker-compose.yml up
```

For the Angular web client:

```bash
cd src/Web/Sales.Web
npm install
npm start
```

For Playwright flows:

```bash
cd tests/Playwright
npm install
npx playwright test
```

## Coding Style & Naming Conventions

C# projects use nullable reference types and implicit usings via `Directory.Build.props`. Use 4-space indentation, file-scoped namespaces where already used, PascalCase for public types/members, camelCase for locals and parameters, and `Async` suffixes for asynchronous methods. Prefer focused extension methods such as `AddSalesInfrastructure` or `AddBuildingBlocksInfrastructure` over large registration blocks.

## Testing Guidelines

Use xUnit for .NET tests. Place tests in the matching project under `tests`, for example `Sales.Domain.Tests` for `Sales.Domain`. Name test classes after the unit under test and use descriptive method names that state behavior. Add focused DI or architecture tests when changing registrations, project references, or shared infrastructure.

## Commit & Pull Request Guidelines

Recent history uses concise imperative commit subjects, for example `Fix Docker NuGet cache race` and `Centralize shared infrastructure registrations`. Keep commits scoped to one logical change. Pull requests should describe the behavior change, list verification commands, mention Docker or migration impact, and include screenshots only for UI changes.

## Security & Configuration Tips

Do not commit secrets. Use local environment variables, user secrets, or compose overrides for connection strings and credentials. Preserve Docker restore caching by copying `.csproj` files before `COPY . .`.
