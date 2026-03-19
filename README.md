# Miniclip Simulator API

A tournament group simulator API built with .NET 10, implementing CQRS pattern with separate read/write databases using MySQL.

## Features

- **Group Generation**: Create tournament groups with random teams (2-6 teams capacity)
- **Match Simulation**: Simulate all matches within a group
- **Standings**: View real-time group standings with match results
- **CQRS Architecture**: Separate read and write models for optimized performance
- **Event Driven**: Domain events with automatic projection updates
- **API Versioning**: Versioned API endpoints
- **Scalar UI**: Interactive API documentation

## Tech Stack

- **.NET 10.0**
- **ASP.NET Core Web API**
- **MySQL 8.0** (Write and Read databases)
- **Entity Framework Core 9.x** with Pomelo MySQL provider
- **MediatR** for CQRS implementation
- **.NET Aspire 13** for local orchestration, observability and service discovery
- **Scalar UI** for interactive API documentation

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (used by Aspire to run the MySQL container)

## Getting Started

### Running with .NET Aspire

1. **Clone the repository**
   ```bash
   git clone https://github.com/nperez0/miniclip.simulator.git
   cd miniclip.simulator/src
   ```

2. **Set the MySQL password in user secrets** (one-time setup)
   ```bash
   cd Miniclip.Simulator.AppHost
   dotnet user-secrets set "Parameters:mysql-password" "<your-password>"
   ```

3. **Run the AppHost**
   ```bash
   dotnet run --project Miniclip.Simulator.AppHost --launch-profile Miniclip.Simulator.AppHost
   ```

   This will:
   - Pull and start a MySQL 8.0 container via Docker
   - Automatically create both `MiniclipSimulator_Write` and `MiniclipSimulator_Read` databases
   - Run EF Core migrations on startup
   - Start the API with all connection strings injected automatically
   - Open the **Aspire Dashboard** at `https://localhost:15888`

4. **Access the application**
   - **Aspire Dashboard**: `https://localhost:15888` (logs, traces, metrics)
   - From the dashboard, the `simulator-api` resource exposes two direct links:
     - **Scalar UI** → interactive API documentation

### Running with Visual Studio

Set `Miniclip.Simulator.AppHost` as the startup project and press **F5**. The Aspire Dashboard will open automatically in your browser.

### Running the API directly (without Aspire)

Requires a MySQL 8.0 instance running locally. Set the connection strings via user secrets:

```bash
cd Miniclip.Simulator.Api
dotnet user-secrets set "ConnectionStrings:SimulatorWrite" "Server=localhost;Port=3306;Database=MiniclipSimulator_Write;User=root;Password=<your-password>;"
dotnet user-secrets set "ConnectionStrings:SimulatorRead"  "Server=localhost;Port=3306;Database=MiniclipSimulator_Read;User=root;Password=<your-password>;"
dotnet run
```

Once running, the API documentation is available at:
- **Scalar UI**: `https://localhost:7087/v1/`
- **OpenAPI JSON**: `https://localhost:7087/openapi/v1.json`

## API Endpoints

### Groups

#### Create Group
```http
POST /api/v1/groups
Content-Type: application/json

{
  "name": "Group A",
  "capacity": 4
}
```
Generates a new group with random teams from the database. Capacity must be between 2 and 6 teams.

**Response**: `204 No Content` with group ID

#### Simulate Group
```http
POST /api/v1/groups/{groupId}/simulate
```
Simulates all matches in the specified group.

**Response**: `204 No Content` on success

#### Get Standings
```http
GET /api/v1/groups/{groupId}/standings
```
Retrieves the current standings for a group.

**Response**: `200 OK` with standings data

## Architecture

The project follows Clean Architecture principles with CQRS:

```
├── Miniclip.Simulator.AppHost                 # .NET Aspire orchestrator
├── Miniclip.Core.ServiceDefaults              # Shared OpenTelemetry & health checks
├── Miniclip.Simulator.Api                     # API layer
├── Miniclip.Simulator.Application.Commands    # Write commands
├── Miniclip.Simulator.Application.Queries     # Read queries
├── Miniclip.Simulator.Domain                  # Domain entities & logic
├── Miniclip.Simulator.ReadModels              # Read models
├── Miniclip.Simulator.ReadModels.Projections  # Event projections
├── Miniclip.Simulator.Infrastructure.Write    # Write database
├── Miniclip.Simulator.Infrastructure.Read     # Read database
└── Miniclip.Core.*                            # Shared core libraries
```

### Database Strategy

- **Write Database** (`MiniclipSimulator_Write`): Stores aggregates
- **Read Database** (`MiniclipSimulator_Read`): Optimized denormalized views for queries
- Automatic synchronisation through event projections

## Observability

When running via Aspire, the **Dashboard** at `https://localhost:15888` provides:

- **Structured logs** from the API in real time
- **Distributed traces** (OpenTelemetry) across requests
- **Metrics** (ASP.NET Core + HTTP client instrumentation)
- **Resource health** status of MySQL and the API

## Troubleshooting

### MySQL container not starting
- Ensure Docker Desktop is running: `docker ps`
- Check the Aspire Dashboard → Resources tab for container status

### API not starting
- Open the Aspire Dashboard → Logs tab and select `simulator-api`
- Verify the MySQL password user secret is set correctly in `Miniclip.Simulator.AppHost`
- If you see a `MissingMethodException` referencing `AbstractionsStrings.ArgumentIsEmpty`, check that EF Core packages in `Directory.Packages.props` are pinned to `9.x` (not `10.x`) — see the note in the Tech Stack section

### Migration issues
- Migrations run automatically on startup via `Database.Migrate()`
- To reset data: stop the AppHost, run `docker volume rm` for the MySQL volume, then restart
