FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Shared/BuildingBlocks.Contracts/BuildingBlocks.Contracts.csproj src/Shared/BuildingBlocks.Contracts/
COPY src/Shared/BuildingBlocks.Observability/BuildingBlocks.Observability.csproj src/Shared/BuildingBlocks.Observability/
COPY Directory.Build.props .
COPY src/Services/AuditLog/AuditLog.Infrastructure/AuditLog.Infrastructure.csproj src/Services/AuditLog/AuditLog.Infrastructure/
COPY src/Services/AuditLog/AuditLog.Worker/AuditLog.Worker.csproj src/Services/AuditLog/AuditLog.Worker/
RUN dotnet restore src/Services/AuditLog/AuditLog.Worker/AuditLog.Worker.csproj
COPY . .
RUN dotnet publish src/Services/AuditLog/AuditLog.Worker/AuditLog.Worker.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "AuditLog.Worker.dll"]
