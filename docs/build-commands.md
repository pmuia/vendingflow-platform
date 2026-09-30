# Build Commands

Run these commands from the repository root unless noted otherwise.

## .NET Services

### API Gateway

```bash
dotnet restore VendingFlow.ApiGateway/VendingFlow.ApiGateway.csproj \
  --source https://api.nuget.org/v3/index.json \
  --source /Users/paulmuia/Projects/Personal/VendingFlow/.nuget-local
```

```bash
dotnet build VendingFlow.ApiGateway/VendingFlow.ApiGateway.csproj --no-restore
```

### Machine Service

```bash
dotnet restore VendingFlow.MachineService/MachineService.sln --disable-parallel --ignore-failed-sources
```

```bash
dotnet build VendingFlow.MachineService/MachineService.sln --no-restore
```

### Inventory Service

```bash
dotnet restore VendingFlow.InventoryService/InventoryService.sln --disable-parallel --ignore-failed-sources
```

```bash
dotnet build VendingFlow.InventoryService/InventoryService.sln --no-restore
```

### Payment Service

```bash
dotnet restore VendingFlow.PaymentService/PaymentService.sln --disable-parallel --ignore-failed-sources
```

```bash
dotnet build VendingFlow.PaymentService/PaymentService.sln --no-restore
```

### Machine Simulator

```bash
dotnet restore VendingFlow.MachineSimulator/MachineSimulator.sln --disable-parallel --ignore-failed-sources
```

```bash
dotnet build VendingFlow.MachineSimulator/MachineSimulator.sln --no-restore
```

## Java Service

### Telemetry Service

```bash
cd VendingFlow.TelemetryService
mvn clean package -DskipTests
```

Or compile without packaging:

```bash
cd VendingFlow.TelemetryService
mvn -DskipTests compile
```

## Web

### Next.js Dashboard

```bash
cd VendingFlow.Web
npm install
npm run build
```

## Quick Verification Set

```bash
dotnet build VendingFlow.ApiGateway/VendingFlow.ApiGateway.csproj --no-restore
dotnet build VendingFlow.MachineService/MachineService.sln --no-restore
dotnet build VendingFlow.InventoryService/InventoryService.sln --no-restore
dotnet build VendingFlow.PaymentService/PaymentService.sln --no-restore
dotnet build VendingFlow.MachineSimulator/MachineSimulator.sln --no-restore
cd VendingFlow.TelemetryService && mvn -DskipTests compile
cd ../VendingFlow.Web && npm run build
```

## Run Commands

Run each service in a separate terminal.

### API Gateway

```bash
dotnet run --project VendingFlow.ApiGateway/VendingFlow.ApiGateway.csproj --urls http://localhost:5000
```

### Machine Service

```bash
dotnet run --project VendingFlow.MachineService/src/Services/MachineService.API/MachineService.API.csproj --urls http://localhost:5001
```

### Inventory Service

```bash
dotnet run --project VendingFlow.InventoryService/src/Services/InventoryService.API/InventoryService.API.csproj --urls http://localhost:5002
```

### Payment Service

```bash
dotnet run --project VendingFlow.PaymentService/src/Services/PaymentService.API/PaymentService.API.csproj --urls http://localhost:5003
```

### Machine Simulator

```bash
dotnet run --project VendingFlow.MachineSimulator/VendingFlow.MachineSimulator.csproj
```

### Telemetry Service

```bash
cd VendingFlow.TelemetryService
mvn spring-boot:run
```

### Next.js Dashboard

```bash
cd VendingFlow.Web
npm install
npm run dev
```

The dashboard runs at:

```text
http://localhost:5173
```

The API Gateway runs at:

```text
http://localhost:5000
```
