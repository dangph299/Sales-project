# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Shared/BuildingBlocks.Contracts/BuildingBlocks.Contracts.csproj src/Shared/BuildingBlocks.Contracts/
COPY src/Shared/BuildingBlocks.Domain/BuildingBlocks.Domain.csproj src/Shared/BuildingBlocks.Domain/
COPY src/Shared/BuildingBlocks.Application/BuildingBlocks.Application.csproj src/Shared/BuildingBlocks.Application/
COPY src/Shared/BuildingBlocks.Observability/BuildingBlocks.Observability.csproj src/Shared/BuildingBlocks.Observability/
COPY src/Shared/BuildingBlocks.Web/BuildingBlocks.Web.csproj src/Shared/BuildingBlocks.Web/
COPY src/Shared/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj src/Shared/BuildingBlocks.Infrastructure/
COPY Directory.Build.props .
COPY src/Services/Inventory/Inventory.Domain/Inventory.Domain.csproj src/Services/Inventory/Inventory.Domain/
COPY src/Services/Inventory/Inventory.Application/Inventory.Application.csproj src/Services/Inventory/Inventory.Application/
COPY src/Services/Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj src/Services/Inventory/Inventory.Infrastructure/
COPY src/Services/Inventory/Inventory.Api/Inventory.Api.csproj src/Services/Inventory/Inventory.Api/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages dotnet restore src/Services/Inventory/Inventory.Api/Inventory.Api.csproj
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish src/Services/Inventory/Inventory.Api/Inventory.Api.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
