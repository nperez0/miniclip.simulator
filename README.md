# Miniclip Simulator

A football group-stage simulator built with **.NET 10** using **CQRS**, **Event Sourcing**, and **read-model projections via Kafka**.

---

## Architecture Overview

```
┌──────────────┐  Commands/Queries  ┌───────────────────────────────────────────┐
│  REST API    │ ──────────────────▶│  Mediator Pipeline                        │
│  (v1)        │                    │  ├─ EventStoreCommandBehavior (commit)    │
└──────────────┘                    │  └─ DomainEventPublisherBehavior (publish)│
                                    └────────────┬──────────────────────────────┘
                                                 │
                     ┌───────────────────────────┼──────────────────────┐
                     ▼                           ▼                      ▼
           ┌──────────────────┐       ┌──────────────────┐    ┌──────────────────┐
           │   EventStoreDB   │       │     Kafka        │    │   Read DB        │
           │  (write / source │       │   (event bus)    │    │   (MySQL)        │
           │   of truth)      │       └────────┬─────────┘    └───────┬──────────┘
           │  · group-{id}    │                │                      │
           │  · team-{id}     │                ▼                      │
           └──────────────────┘       ┌────────────────────┐          │
                                      │ ProjectionsConsumer│──────────▶
                                      │  (per event type)  │
                                      └────────────────────┘
```

### Write side — Event Sourcing (EventStoreDB)

Every state change is stored as an immutable domain event. Aggregates are rebuilt by replaying their event stream.

| Aggregate | Stream pattern | Events |
|-----------|---------------|--------|
| `Group`   | `group-{id}`  | `GroupCreated`, `TeamAdded`, `MatchScheduled`, `MatchPlayed` |
| `Team`    | `team-{id}`   | `TeamRegistered` |

`GetAllAsync` reads the `$ce-{type}` [category stream](https://developers.eventstore.com/server/v24.10/projections.html#by-category) created automatically by EventStoreDB's built-in `$by_category` projection.

### Read side — Projections (MySQL + Kafka)

Read models are stored in a separate MySQL database and built asynchronously from Kafka events. Each `ProjectionsConsumerService<TEvent>` consumes a single topic and dispatches the event to Mediator notification handlers, which update the read DB.

Idempotency is guaranteed by recording each processed `event-id` + consumer group ID in a `ProcessedEvents` table before committing the read-side transaction.

---

## Projects

```
src/
├── Miniclip.Core/                              # Primitives: Result<T>, extension methods
├── Miniclip.Core.Domain/                       # AggregateRoot, IAggregateRepository<T>, IDomainEvent
├── Miniclip.Core.Application/                  # IEventBus, pipeline behaviour base types
├── Miniclip.Core.EF/                           # EF Core base types (IReadModelUnitOfWork)
├── Miniclip.Core.EventSourcing/                # IEventStore<T>, IEventStoreSession, AggregateRepository<T>
├── Miniclip.Core.EventSourcing.EventStoreDB/   # EventStoreDbEventStore<T>, SystemTextJsonEventSerializer
├── Miniclip.Core.Kafka/                        # KafkaConsumerService, KafkaEventBus, TopicNaming
├── Miniclip.Core.ReadModels/                   # IReadModelUnitOfWork, projection handler base types
│
├── Miniclip.Simulator.Domain/                  # Group + Team aggregates, domain services, value objects
├── Miniclip.Simulator.Application.Commands/    # Command handlers: GenerateGroup, SimulateGroup
├── Miniclip.Simulator.Application.Queries/     # Query handlers: GetGroupStandings, GetMatchResults
├── Miniclip.Simulator.ReadModels/              # Projection handlers, read-model repository interfaces
├── Miniclip.Simulator.ReadModels.Projections/  # ProjectionsConsumerService<TEvent>
├── Miniclip.Simulator.Infrastructure.Read/     # EF read DbContext, repository implementations
│
├── Miniclip.Simulator.Api/                     # ASP.NET Core host — controllers, DI wiring, seeder
├── Miniclip.Simulator.AppHost/                 # .NET Aspire orchestration (MySQL, EventStoreDB, Kafka)
└── Miniclip.Core.ServiceDefaults/              # Shared OpenTelemetry & health checks
```

---

## Key Patterns

### Mediator pipeline (write side)

```
Command
  └▶ EventStoreCommandBehavior       ← commits the IEventStoreSession after the handler returns
       └▶ CommandHandler
            └▶ AggregateRepository.Add(aggregate)   ← tracks uncommitted events
  └▶ DomainEventPublisherBehavior    ← publishes committed events to Kafka
```

### Kafka consumer pattern

`KafkaConsumerService` is an abstract `BackgroundService` that owns the Confluent consumer lifecycle, including subscribe, the consume loop, and the retry/dead-letter policy. Subclasses provide the consumer group ID and the consumer instance:

```csharp
// Abstract base — KafkaConsumerService builds its own IConsumer<string,byte[]>
public abstract class KafkaConsumerService(string[] topics, IConfiguration configuration, ...) : BackgroundService
{
    protected abstract string ConsumerGroupId { get; }
    protected abstract IConsumer<string, byte[]> BuildConsumer(ConsumerConfig config);
    protected abstract Task HandleAsync(ConsumeResult<string, byte[]> result, CancellationToken ct);
}

// Concrete — ProjectionsConsumerService<TEvent> handles one event type
public class ProjectionsConsumerService<TEvent> : KafkaConsumerService { ... }
```

> **Why `IServiceScopeFactory` and not direct injection?**
>
> `BackgroundService` is a singleton. `IReadModelUnitOfWork` and `IPublisher` are scoped.
> Injecting scoped services directly — either via the constructor or via the `AddHostedService`
> factory lambda — captures them for the application lifetime, making the `DbContext` effectively
> a singleton (thread-safety issues, stale data). `ProjectionsConsumerService` instead injects
> `IServiceScopeFactory` and creates a **fresh scope per message** inside `HandleAsync`, giving
> each message its own clean unit of work and transactional boundary.

### Team seeding

`TeamDataSeeder` (`IHostedService`) writes the predefined squad of 10 teams to EventStoreDB on startup. It is idempotent: each team ID is checked with `FindAsync` before writing, so re-running never produces duplicate events.

---

## Tech Stack

| Concern | Technology |
|---------|-----------|
| Runtime | .NET 10 |
| API | ASP.NET Core Web API with API versioning |
| CQRS mediator | [Mediator](https://github.com/martinothamar/Mediator) (source-generator based) |
| Event store | EventStoreDB 24.10 |
| Message broker | Apache Kafka (Confluent .NET client) |
| Read database | MySQL 8 via EF Core 9 + Pomelo provider |
| Orchestration | .NET Aspire 9 |
| API docs | Scalar UI |
| Testing | NUnit + NSubstitute + Shouldly |

---

## Getting Started

**Prerequisites:** .NET 10 SDK, Docker Desktop.

### With .NET Aspire (recommended)

```bash
git clone https://github.com/nperez0/miniclip.simulator.git
cd miniclip.simulator/src/Miniclip.Simulator.AppHost

# One-time: set the MySQL password
dotnet user-secrets set "Parameters:mysql-password" "<your-password>"

dotnet run
```

Aspire starts MySQL, EventStoreDB, Kafka, and the API. EF Core migrations and team seeding run automatically. The Aspire Dashboard opens at `https://localhost:15888`.

### With Visual Studio

Set `Miniclip.Simulator.AppHost` as the startup project and press **F5**.

### API only (no Aspire)

```bash
cd src/Miniclip.Simulator.Api
dotnet user-secrets set "ConnectionStrings:SimulatorRead"  "Server=localhost;..."
dotnet user-secrets set "ConnectionStrings:EventStore"     "esdb://localhost:2113?tls=false"
dotnet user-secrets set "ConnectionStrings:kafka"          "localhost:9092"
dotnet run
```

---

## API

All endpoints are under `/api/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/groups` | Generate a group — picks `capacity` random teams from EventStoreDB |
| `POST` | `/api/v1/groups/{id}/simulate` | Simulate all unplayed matches in a group |
| `GET`  | `/api/v1/groups/{id}/standings` | Get current standings for a group (read model) |

#### Generate a group

```http
POST /api/v1/groups
Content-Type: application/json

{ "name": "Group A", "capacity": 4 }
```

`capacity` must be between 2 and 6. Returns `200 OK` with the new group''s `Guid`.

---

## Testing

```bash
dotnet test
```

| Project | Type | Covers |
|---------|------|--------|
| `Miniclip.Simulator.Domain.UnitTests` | Unit | Aggregates, match simulation, domain services |
| `Miniclip.Simulator.Application.Commands.UnitTests` | Unit | Command handlers |
| `Miniclip.Simulator.Application.Queries.UnitTests` | Unit | Query handlers |
| `Miniclip.Core.Kafka.UnitTests` | Unit | `KafkaConsumerService` — retry policy and dead-letter routing |
| `Miniclip.Simulator.ReadModels.Projections.UnitTests` | Unit | `ProjectionsConsumerService` — idempotency, projection handlers |
| `Miniclip.Simulator.ReadModels.Projections.IntegrationTests` | Integration | Full projection pipeline against a real read DB |

---

## Observability

The Aspire Dashboard (`https://localhost:15888`) provides structured logs, distributed traces (OpenTelemetry), metrics, and resource health status for all services.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| MySQL container not starting | Ensure Docker is running: `docker ps` |
| API fails to start | Check Aspire Dashboard → Logs → `simulator-api`; verify the MySQL password user secret |
| EventStoreDB `$ce-team` stream not found | Confirm `EVENTSTORE_RUN_PROJECTIONS=All` and `EVENTSTORE_START_STANDARD_PROJECTIONS=true` are set (already configured in AppHost) |
| Stale read-model data | Confirm Kafka consumer is running (Dashboard → Traces); check `ProcessedEvents` table for duplicate event IDs |
