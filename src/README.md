# Miniclip Simulator API

A tournament group simulator API built with .NET 8, implementing CQRS pattern with separate read/write databases using MySQL.

## Features

- **Group Generation**: Create tournament groups with random teams (2-6 teams capacity)
- **Match Simulation**: Simulate all matches within a group
- **Standings**: View real-time group standings with match results
- **CQRS Architecture**: Separate read and write models for optimized performance
- **Event Driven**: Domain events with automatic projection updates
- **API Versioning**: Versioned API endpoints
- **OpenAPI/Swagger**: Interactive API documentation

## Tech Stack

- **.NET 8.0**
- **ASP.NET Core Web API**
- **MySQL 8.0** (Write and Read databases)
- **Entity Framework Core** with Pomelo MySQL provider
- **MediatR** for CQRS implementation
- **Docker & Docker Compose** for containerization
- **Swagger/OpenAPI** for API documentation

## Prerequisites

- [Docker](https://www.docker.com/products/docker-desktop/) (version 20.10 or later)
- [Docker Compose](https://docs.docker.com/compose/install/) (version 1.29 or later)

## Getting Started

### Running with Docker Compose

1. **Clone the repository**
   ```bash
   git clone https://github.com/nperez0/miniclip.simulator.git
   cd miniclip.simulator/src
   ```

2. **Start the application**
   ```bash
   docker-compose up -d
   ```

   This will:
   - Start a MySQL 8.0 database container
   - Build and start the API container
   - Automatically run database migrations
   - Expose the API on port 5000

3. **Access the application**
   - **API Base URL**: http://localhost:5000
   - **Swagger UI**: http://localhost:5000/swagger
   - **MySQL Database**: localhost:4306 (root/root)

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
├── Miniclip.Simulator.Api              # API layer
├── Miniclip.Simulator.Application.Commands    # Write commands
├── Miniclip.Simulator.Application.Queries     # Read queries
├── Miniclip.Simulator.Domain           # Domain entities & logic
├── Miniclip.Simulator.ReadModels       # Read models
├── Miniclip.Simulator.ReadModels.Projections  # Event projections
├── Miniclip.Simulator.Infrastructure.Write    # Write database
├── Miniclip.Simulator.Infrastructure.Read     # Read database
└── Miniclip.Core.*                     # Shared core libraries
```

### Database Strategy

- **Write Database** (`MiniclipSimulator_Write`): Stores aggregates
- **Read Database** (`MiniclipSimulator_Read`): Optimized denormalized views for queries
- Automatic synchronization through event projections

## Development

### Running Locally (without Docker)

1. **Setup MySQL**
   - Ensure MySQL 8.0 is running on port 4306
   - Update connection strings in `appsettings.json`

2. **Run the API**
   ```bash
   cd src/Miniclip.Simulator.Api
   dotnet run

## Configuration

### Environment Variables

When running with Docker Compose, the following environment variables are configured:

- `ASPNETCORE_ENVIRONMENT`: Development
- `ConnectionStrings__SimulatorWrite`: Write database connection
- `ConnectionStrings__SimulatorRead`: Read database connection

### Docker Compose Services

- **mysql**: MySQL 8.0 database server (port 4306)
- **api**: .NET 8 API application (port 5000)

## Troubleshooting

### Database Connection Issues
- Ensure MySQL container is healthy: `docker-compose ps`
- Check logs: `docker-compose logs mysql`

### API Not Starting
- Check API logs: `docker-compose logs api`
- Ensure port 5000 is not already in use

### Migration Issues
- Migrations run automatically on startup
- To reset: `docker-compose down -v` then `docker-compose up -d`
