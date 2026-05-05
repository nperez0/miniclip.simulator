# Miniclip Simulator

A football group-stage simulator built with **.NET 10** using **CQRS**, **Event Sourcing**, and **read-model projections via Kafka**.

---

## Architecture Overview

```
┌──────────────┐  Commands/Queries  ┌────────────────────────────────────────────────┐
│  REST API    │ ──────────────────▶│  Mediator Pipeline                             │
│  (v1)        │                    │  ├─ LoggingBehavior (timing + OTel tagging)   │
└──────────────┘                    │  └─ EventStoreCommandBehavior (commit+publish) │
                                    └────────────┬───────────────────────────────────┘
                                                 │
                     ┌───────────────────────────┼──────────────────────┐
                     ▼                           ▼                      ▼
           ┌──────────────────┐       ┌──────────────────┐    ┌──────────────────┐
           │   KurrentDB      │       │     Kafka        │    │   Read DB        │
           │  (write / source │       │   (event bus)    │    │   (MySQL)        │
           │   of truth)      │       └────────┬─────────┘    └───────┬──────────┘
           │  · group-{id}    │                │                      │
           │  · team-{id}     │                ▼                      │
           └──────────────────┘       ┌────────────────────┐          │
                                      │ ReadModels WebJob  │          │
                                      │ ProjectionsConsumer│──────────▶
                                      │  (per aggregate)   │
                                      └────────────────────┘
```

> **KurrentDB** is the renamed EventStoreDB. The client library is `KurrentDB.Client`; the Docker image is `kurrentplatform/kurrentdb`.

### Write side — Event Sourcing (KurrentDB)

Every state change is stored as an immutable domain event. Aggregates are rebuilt by replaying their event stream.

| Aggregate | Stream pattern | Events |
|-----------|---------------|--------|
| `Group`   | `group-{id}`  | `GroupCreated`, `TeamAdded`, `MatchScheduled`, `MatchPlayed` |
| `Team`    | `team-{id}`   | `TeamRegistered` |

`GetAllAsync` reads the `$ce-{type}` [category stream](https://docs.kurrent.io/server/v25/projections.html#by-category) created automatically by KurrentDB's built-in `$by_category` projection.

### Write-side pipeline (Mediator)

Registration order (outermost first):
1. `LoggingBehavior` — logs request timing and domain errors; tags the active OTel span on conflicts.
2. `EventStoreCommandBehavior` — commits the `IEventStoreSession` after the handler succeeds, then calls `ICommittedEventPublisher.PublishAsync()` for each committed event. `CommittedEventPublisher` maps domain events to integration events via `IIntegrationEventMapperRegistry` before publishing to `IEventBus` (Kafka). Events without a registered mapper are silently skipped.

### Read side — Projections (MySQL + Kafka)

Projection consumers run in a separate **ReadModels WebJob** (`Miniclip.Simulator.ReadModels.WebJob`). A single `KafkaConsumerHost` instance subscribes to each aggregate topic; `ProjectionMessageHandler<TEvent>` dispatches deserialized events to ordered `INotificationHandler` handlers that update the MySQL read DB.

Idempotency is guaranteed by recording each processed `event-id` + consumer group ID in a `ProcessedEvents` table before committing the read-side transaction.

---

## Projects

```
src/
├── Miniclip.Core/                              # Primitives: Result<T>, extension methods
├── Miniclip.Core.Domain/                       # AggregateRoot, IAggregateRepository<T>, IDomainEvent
├── Miniclip.Core.Application/                  # ICommand, IQuery, pipeline behaviours, DomainEventJsonSerializer
├── Miniclip.Core.EF/                           # EF Core base types (IReadModelUnitOfWork)
├── Miniclip.Core.EventSourcing/                # IEventStore<T>, IEventStoreSession, AggregateRepository<T>
├── Miniclip.Core.EventSourcing.EventStoreDB/   # EventStoreDbEventStore<T>, SystemTextJsonEventSerializer
├── Miniclip.Core.Messaging/                    # IEventBus, IMessagePipeline, IMessageHandler<T>,
│                                               #   IMessageMiddleware, IRetryPolicy, IDeadLetterHandler,
│                                               #   ExponentialBackoffRetryPolicy, MessageHeaders
├── Miniclip.Core.Messaging.Pipeline/           # MessagePipeline, TracingMiddleware, LoggingMiddleware,
│                                               #   RetryMiddleware, MessageHandlerRegistry
├── Miniclip.Core.Messaging.Kafka/              # KafkaConsumerHost, KafkaEventBus, KafkaDeadLetterHandler,
│                                               #   TopicNaming, ConsumerGroupIdNaming, KafkaMessageMapper
├── Miniclip.Core.OpenTelemetry/                # OpenTelemetryActivity, OpenTelemetryMetrics, OTel extensions
├── Miniclip.Core.ReadModels/                   # IReadModelUnitOfWork, projection handler base types
├── Miniclip.Core.ReadModels.Projections/       # [HandlerPriority] attribute, IProjectionDispatcher,
│                                               #   ordered projection execution
├── Miniclip.Core.ServiceDefaults/              # Serilog structured logging (AddStructuredLogging)
│
├── Miniclip.Simulator.Domain/                  # Group + Team aggregates, domain services, value objects
├── Miniclip.Simulator.Application.Commands/    # Command handlers: GenerateGroup, SimulateGroup
├── Miniclip.Simulator.Application.Queries/     # Query handlers: GetGroupStandings, GetMatchResults
├── Miniclip.Simulator.ReadModels/              # Read model POCOs and repository interfaces
├── Miniclip.Simulator.ReadModels.Projections/  # ProjectionMessageHandler<TEvent>,
│                                               #   GroupStandingsProjection, MatchResultProjection
├── Miniclip.Simulator.IntegrationEvents/       # MatchPlayedIntegrationEvent, MatchPlayedIntegrationEventMapper
├── Miniclip.Simulator.Infrastructure.Read/     # EF read DbContext, repository implementations
│
├── Miniclip.Simulator.Api/                     # ASP.NET Core host — controllers, CorrelationIdMiddleware, DI wiring, TeamDataSeeder
├── Miniclip.Simulator.ReadModels.WebJob/       # Worker Service — projection consumers, read DB migrations
└── Miniclip.Simulator.AppHost/                 # .NET Aspire orchestration (MySQL, KurrentDB, Kafka, services)
```

---

## Key Patterns

### Mediator pipeline (write side)

```
Command
  └▶ LoggingBehavior                        ← logs timing; tags OTel span on domain errors
       └▶ EventStoreCommandBehavior
            ├── session.CommitAsync()        ← appends events to KurrentDB
            ├── eventBus.PublishAsync()      ← publishes committed events to Kafka
            └▶ CommandHandler
                 └▶ AggregateRepository.Add(aggregate)   ← tracks uncommitted events
```

### Kafka messaging pipeline (read side)

`KafkaConsumerHost` is a `BackgroundService` (one instance per consumer group). It subscribes to a Kafka topic and forwards each message through the `IMessagePipeline` — a middleware chain registered outermost-first:

```
KafkaConsumerHost
  └▶ IMessagePipeline
       ├── TracingMiddleware    ← starts OTel span; tags message-id, type, subscription-id
       ├── LoggingMiddleware    ← logs receipt and outcome
       └── RetryMiddleware      ← exponential back-off; dead-letters on exhaustion
              └▶ ProjectionMessageHandler<TEvent>   ← IMessageHandler<TEvent>
                   ├── idempotency check (ProcessedEvents table)
                   └── IProjectionDispatcher
                         ├── MatchResultProjection    (priority 1)
                         └── GroupStandingsProjection (priority 2)
```

> **Topic naming:** `simulator.{aggregate-kebab-case}` — e.g. `Group` → `simulator.group`  
> **Consumer group:** `simulator-readmodels-webjob-group` (single group handles all projection topics)

> **Why `IServiceScopeFactory`?** `ProjectionMessageHandler<TEvent>` creates a **fresh DI scope per message**, giving each message its own `DbContext` and transactional boundary.

### Correlation ID

`CorrelationIdMiddleware` (ASP.NET Core) reads `X-Correlation-Id` from the request (or generates a fresh `Guid`) and stores it on `IMutablePropagationContext`. The ID propagates to all downstream Kafka messages, enabling end-to-end tracing across services. The correlation ID is echoed back in the response header.

### Observability

- **Structured logs** — both the API and WebJob use `builder.AddStructuredLogging()` (Serilog, console JSON, optional OTLP sink).
- **Distributed traces** — `OpenTelemetryActivity.StartActivity` wraps each Kafka message. KurrentDB, ASP.NET Core, MySQL, and Kafka instrumentation all emit spans.
- **Metrics** — `kafka.retry.attempts` and `kafka.messages.failed` counters from the `Miniclip.Simulator.Kafka` meter, exported via OTLP.

### Team seeding

`TeamDataSeeder` (`IHostedService`) writes the predefined squad of 10 teams to KurrentDB on startup. It is idempotent: each team ID is checked with `FindAsync` before writing, so re-running never produces duplicate events.

---

## Tech Stack

| Concern | Technology |
|---------|-----------|
| Runtime | .NET 10 |
| API | ASP.NET Core Web API with API versioning |
| CQRS mediator | [Mediator](https://github.com/martinothamar/Mediator) (source-generator based) |
| Event store | KurrentDB (formerly EventStoreDB) |
| Message broker | Apache Kafka (Confluent .NET client) |
| Read database | MySQL 8 via EF Core 9 + Pomelo provider |
| Orchestration | .NET Aspire 9 |
| Observability | OpenTelemetry (OTLP) + Serilog |
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

Aspire starts MySQL, KurrentDB, Kafka (with Kafka UI), the ReadModels WebJob, and the API. The WebJob runs EF Core read-DB migrations and waits for Kafka topics before the API starts. Team seeding runs in the API on startup. The Aspire Dashboard opens at `https://localhost:15888`.

### With Visual Studio

Set `Miniclip.Simulator.AppHost` as the startup project and press **F5**.

### API only (no Aspire)

```bash
cd src/Miniclip.Simulator.Api
dotnet user-secrets set "ConnectionStrings:SimulatorRead"  "Server=localhost;..."
dotnet user-secrets set "ConnectionStrings:EventStore"     "kurrentdb://localhost:2113?tls=false"
dotnet user-secrets set "ConnectionStrings:kafka"          "localhost:9092"
dotnet run
```

---

## API

All endpoints are under `/api/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/groups` | Generate a group — picks `capacity` random teams from KurrentDB |
| `POST` | `/api/v1/groups/{id}/simulate` | Simulate all unplayed matches in a group |
| `GET`  | `/api/v1/groups/{id}/standings` | Get current standings for a group (read model) |

#### Generate a group

```http
POST /api/v1/groups
Content-Type: application/json

{ "name": "Group A", "capacity": 4 }
```

`capacity` must be between 2 and 6. Returns `200 OK` with the new group's `Guid`.

---

## Testing

```bash
dotnet test
```

| Project | Type | Covers |
|---------|------|--------|
| `Miniclip.Simulator.Domain.UnitTests` | Unit | Aggregate logic, fixture scheduling, simulation algorithm |
| `Miniclip.Simulator.Application.Commands.UnitTests` | Unit | Command handler logic |
| `Miniclip.Simulator.Application.Queries.UnitTests` | Unit | Query handler logic |
| `Miniclip.Simulator.ReadModels.Projections.UnitTests` | Unit | `ProjectionMessageHandler` idempotency; projection handlers |
| `Miniclip.Simulator.ReadModels.Projections.IntegrationTests` | Integration | Full projection pipeline against a real read DB |
| `Miniclip.Core.Messaging.Kafka.UnitTests` | Unit | _(empty — tests migrated to messaging projects)_ |
| `Miniclip.Simulator.Api.UnitTests` | Unit | Controller / result extension behaviour |
| `Miniclip.Simulator.Common.Tests` | Shared | Test helpers and builders |
| `Miniclip.Core.Tests` | Unit | Shared kernel tests |

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| MySQL container not starting | Ensure Docker is running: `docker ps` |
| API fails to start | Check Aspire Dashboard → Logs → `simulator-api`; verify the MySQL password user secret |
| KurrentDB `$ce-group` stream not found | Confirm `KURRENTDB_RUN_PROJECTIONS=All` and `KURRENTDB_START_STANDARD_PROJECTIONS=true` are set (already configured in AppHost) |
| Stale read-model data | Confirm the WebJob is running (Dashboard → Resources); check `ProcessedEvents` table for duplicate event IDs |
