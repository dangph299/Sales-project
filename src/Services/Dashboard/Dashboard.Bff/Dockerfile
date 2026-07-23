# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Sales.sln .
COPY Directory.Build.props .
COPY src/Shared/BuildingBlocks.Contracts/BuildingBlocks.Contracts.csproj src/Shared/BuildingBlocks.Contracts/
COPY src/Shared/BuildingBlocks.Domain/BuildingBlocks.Domain.csproj src/Shared/BuildingBlocks.Domain/
COPY src/Shared/BuildingBlocks.Application/BuildingBlocks.Application.csproj src/Shared/BuildingBlocks.Application/
COPY src/Shared/BuildingBlocks.Observability/BuildingBlocks.Observability.csproj src/Shared/BuildingBlocks.Observability/
COPY src/Shared/BuildingBlocks.Web/BuildingBlocks.Web.csproj src/Shared/BuildingBlocks.Web/
COPY src/Shared/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj src/Shared/BuildingBlocks.Infrastructure/
COPY src/Services/Dashboard/Dashboard.Bff/Dashboard.Bff.csproj src/Services/Dashboard/Dashboard.Bff/
RUN --mount=type=cache,id=dashboard-bff-nuget,target=/root/.nuget/packages,sharing=locked dotnet restore src/Services/Dashboard/Dashboard.Bff/Dashboard.Bff.csproj
COPY . .
RUN --mount=type=cache,id=dashboard-bff-nuget,target=/root/.nuget/packages,sharing=locked dotnet publish src/Services/Dashboard/Dashboard.Bff/Dashboard.Bff.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# curl is used by the container HEALTHCHECK to probe the /health endpoint.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Dashboard.Bff.dll"]
