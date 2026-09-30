# Migration Commands

Run these commands from the repository root unless noted otherwise.

## .NET Services

### Add Migrations

```bash
dotnet ef migrations add InitialCreate \
  --project VendingFlow.MachineService/src/Infrastructure/Core.Infrastructure/Core.Infrastructure.csproj \
  --startup-project VendingFlow.MachineService/src/Services/MachineService.API/MachineService.API.csproj \
  --context MachineDbContext
```

```bash
dotnet ef migrations add InitialCreate \
  --project VendingFlow.InventoryService/src/Infrastructure/Core.Infrastructure/Core.Infrastructure.csproj \
  --startup-project VendingFlow.InventoryService/src/Services/InventoryService.API/InventoryService.API.csproj \
  --context InventoryDbContext
```

```bash
dotnet ef migrations add InitialCreate \
  --project VendingFlow.PaymentService/src/Infrastructure/Core.Infrastructure/Core.Infrastructure.csproj \
  --startup-project VendingFlow.PaymentService/src/Services/PaymentService.API/PaymentService.API.csproj \
  --context PaymentDbContext
```

### Apply Migrations

```bash
dotnet ef database update \
  --project VendingFlow.MachineService/src/Infrastructure/Core.Infrastructure/Core.Infrastructure.csproj \
  --startup-project VendingFlow.MachineService/src/Services/MachineService.API/MachineService.API.csproj \
  --context MachineDbContext
```

```bash
dotnet ef database update \
  --project VendingFlow.InventoryService/src/Infrastructure/Core.Infrastructure/Core.Infrastructure.csproj \
  --startup-project VendingFlow.InventoryService/src/Services/InventoryService.API/InventoryService.API.csproj \
  --context InventoryDbContext
```

```bash
dotnet ef database update \
  --project VendingFlow.PaymentService/src/Infrastructure/Core.Infrastructure/Core.Infrastructure.csproj \
  --startup-project VendingFlow.PaymentService/src/Services/PaymentService.API/PaymentService.API.csproj \
  --context PaymentDbContext
```

## Java Telemetry Service

Telemetry uses Flyway. Start the Spring Boot service to apply migrations:

```bash
cd VendingFlow.TelemetryService
mvn spring-boot:run
```

Flyway applies SQL files from:

```text
src/main/resources/db/migration/
```

Current migration:

```text
src/main/resources/db/migration/V1__telemetry.sql
```

If you want to run Flyway through Maven directly, add the Flyway Maven plugin first. Currently, Spring Boot startup is the migration path.
