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
COPY src/Services/Sales/Sales.Domain/Sales.Domain.csproj src/Services/Sales/Sales.Domain/
COPY src/Services/Sales/Sales.Application/Sales.Application.csproj src/Services/Sales/Sales.Application/
COPY src/Services/Sales/Sales.Infrastructure/Sales.Infrastructure.csproj src/Services/Sales/Sales.Infrastructure/
COPY src/Services/Sales/Sales.Api/Sales.Api.csproj src/Services/Sales/Sales.Api/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages dotnet restore src/Services/Sales/Sales.Api/Sales.Api.csproj
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish src/Services/Sales/Sales.Api/Sales.Api.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Sales.Api.dll"]
