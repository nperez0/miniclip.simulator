# Architecture

## Overview

Miniclip Simulator is built on **Clean Architecture** with **CQRS**, **DDD**, and **event-driven projections**.
The goal is a clear separation between business rules (domain), orchestration (application), and infrastructure (persistence, API).

---

## Layers

### Shared Kernel — `Miniclip.Core` & `Miniclip.Core.Domain`

These projects have zero dependencies on any simulator-specific logic.

| Component | Purpose |
|---|---|
| `Result<T>` | Discriminated union for success/failure. Replaces exceptions for business rule violations. |
| `ExceptionBase` | Base type for all typed exceptions across the solution. |
| `AggregateRoot` | Base class for all DDD aggregates. Owns the event queue (`Enqueue`), version tracking, and replay (`ReplayEvent` / `Apply`). |
| `IDomainEvent` | Marker interface implemented by all domain events. |
| `IAggregateRepository<T>` | Generic write-side repository interface. Implemented by `AggregateRepository<T>` backed by `IEventStore<T>`. |

### Observability — `Miniclip.Core.OpenTelemetry` & `Miniclip.Core.ServiceDefaults`

Cross-cutting observability infrastructure shared by the API and the WebJob.

| Component | Purpose |
|---|---|
| `OpenTelemetryActivity` | Wraps `ActivitySource` to start named spans; records exceptions with `NoticeError`. |
| `OpenTelemetryMetrics` | Exposes `RecordRetryAttempt()` / `RecordMessageFailed()` counters on the `Miniclip.Simulator.Kafka` meter. |
| `OpenTelemetryConstants` | Meter and activity source names. |
| `TraceProviderBuilderExtensions` | `AddSimulator()`, `AddMySqlData()`, `AddMySqlConnector()` extension methods. |
| `MeterProviderBuilderExtensions` | `AddSimulator()` extension method. |
| `SerilogConfiguration` | `AddStructuredLogging()` — configures Serilog with console JSON and optional OTLP log sink. |

### Domain — `Miniclip.Simulator.Domain`

Pure business logic. No dependencies on ASP.NET Core, EF Core, or any infrastructure library.

```
Aggregates/
├── Groups/
│   ├── Entities/         Group.cs, Match.cs
│   ├── Events/           GroupCreated.cs, TeamAdded.cs, MatchScheduled.cs, MatchPlayed.cs
│   ├── Exceptions/       GroupCreationException, GroupAddTeamException, ...
│   ├── ValueObjects/     TeamInfo.cs  (Id, Name, Strength snapshot)
│   └── Services/
│       ├── Fixtures/     IFixtureScheduler, RoundRobinScheduler, FixtureSchedulerService
│       └── Simulator/    IMatchSimulator, MatchSimulator, GroupSimulator
└── Teams/
    ├── Entities/         Team.cs
    ├── Events/           TeamRegistered.cs
    └── Exceptions/       TeamCreationException.cs
```

### Application — Commands & Queries

Split into two projects to enforce CQRS.

**`Miniclip.Simulator.Application.Commands`** (write side)
- Depends on `Miniclip.Simulator.Domain` and `IAggregateRepository<T>`.
- Handlers: `GenerateGroupCommandHandler`, `SimulateGroupCommandHandler`.
- `GenerateGroupCommandHandler` loads all teams from KurrentDB via `IAggregateRepository<Team>.GetAllAsync()`, picks a random subset, and creates a `Group`.

**`Miniclip.Simulator.Application.Queries`** (read side)
- Depends only on read model repository interfaces (`IGroupStandingsRepository`, `IMatchResultsRepository`).
- Handlers: `GroupStandingsQueryHandler`.
- Never touches the write `DbContext` or domain aggregates.

### Read Models & Projections

**`Miniclip.Simulator.ReadModels`** — POCO read model definitions (`GroupStandingsModel`, `MatchResultModel`) and repository interfaces.

**`Miniclip.Simulator.ReadModels.Projections`** — `ProjectionMessageHandler<TEvent>` (`IMessageHandler<TEvent>`) plus `IProjectionHandler<TEvent>` projections dispatched in priority order via `IProjectionDispatcher`.

Projections are the **only** writers to the read database. They run in priority order via `[HandlerPriority(n)]`:

| Priority | Projection | What it does |
|---|---|---|
| 1 | `MatchResultProjection` | Creates a `MatchResultModel` row for the played match. |
| 2 | `GroupStandingsProjection` | Gets or creates standings rows for both teams, updates all stats, then recalculates positions. |

### Infrastructure — Read

The **write side** is KurrentDB. The current solution keeps EF Core only on the read side.

`SimulatorReadDbContext` holds all read models and exposes both read and write repositories:

| Context | Project | Tracks |
|---|---|---|
| `SimulatorReadDbContext` | `Infrastructure.Read` | `GroupStandingsModel`, `MatchResultModel`, `ProcessedEventsModel` |

### API — `Miniclip.Simulator.Api`

ASP.NET Core Web API. Versioned using `Asp.Versioning`.

```
Controllers/V1/    GroupsController
Extensions/        ResultExtensions  (Result<T> → IActionResult mapping)
Infrastructure/
  Configuration/   EventStoreConfiguration, ReadModelsConfiguration,
                   MediatorConfiguration, DomainConfiguration,
                   ApiVersioningConfiguration, OpenTelemetryConfiguration,
                   WebApplicationConfiguration
  Middleware/      CorrelationIdMiddleware  (X-Correlation-Id header; propagates to Kafka messages)
  Seeding/         TeamDataSeeder  (seeds 10 teams to KurrentDB on startup)
Startup.cs         ConfigureServices + Configure
Program.cs         Host builder entry point
```

**Mediator pipeline (write side):**
1. `LoggingBehavior` (outermost) — logs request timing; tags active OTel span on domain errors.
2. `EventStoreCommandBehavior` — commits `IEventStoreSession` to append pending events to KurrentDB.

### EventRelay WebJob — `Miniclip.Simulator.EventRelay.WebJob`

A **.NET Worker Service** that forwards committed domain events from KurrentDB to Kafka.

```
Infrastructure/
  Configuration/   EventRelayConfiguration      (KurrentDB + mapper registry + hosted service)
                   OpenTelemetryConfiguration
                   HealthCheckConfiguration
KurrentDbForwarderService.cs                    (persistent subscription consumer)
Startup.cs
Program.cs         Host builder entry point (AddStructuredLogging)
```

`KurrentDbForwarderService` subscribes to `simulator-kurrentdb-to-kafka-forwarder`, deserializes domain events, maps them via `IIntegrationEventMapperRegistry`, and publishes integration events to Kafka through `IEventBus`.

### ReadModels WebJob — `Miniclip.Simulator.ReadModels.WebJob`

A **.NET Worker Service** (`Microsoft.NET.Sdk.Worker`) that is the single owner of all Kafka projection consumers and the read-model database.

```
Infrastructure/
  Configuration/   ReadModelsConfiguration      (DbContext + write repositories + InitializeDatabases)
                   KafkaConfiguration           (AddKafka + consumer descriptor)
                   ProjectionsConfiguration     (IRecalculatePositionService)
                   HealthCheckConfiguration     (HealthCheckHttpServerService)
                   OpenTelemetryConfiguration
Startup.cs         ConfigureServices + Configure
Program.cs         Host builder entry point (AddStructuredLogging)
```

The WebJob starts before the API in Aspire (`WaitFor(webjob)`):
1. Runs EF Core read-DB migrations via `host.InitializeDatabases()`.
2. Registers `KafkaConsumerHost` (for `Group`) as a hosted service.
3. On `ExecuteAsync`, the host subscribes to `simulator.group` and processes each message through the `IInboundPipeline`.

### Orchestration — `Miniclip.Simulator.AppHost`

.NET Aspire AppHost. Provisions:
- A **MySQL** container.
- A **KurrentDB** container (`kurrentplatform/kurrentdb`) with `$by_category` and standard projections enabled.
- A **Kafka** container with **Kafka UI**.
- A **`KafkaTopicsResource`** (`WithTopicCreation()`) that auto-creates the `simulator.group` topic via the Kafka admin client before any service starts.
- The **ReadModels WebJob** project as a service (starts before the API).
- The **EventRelay WebJob** project as a service (waits for KurrentDB and Kafka topics).
- The **API** project as a service (waits for KurrentDB, read DB, both WebJobs, and Kafka topics).

---

## Request Flow

### Write — Generate Group

```
POST /api/v1/groups
        │
        ▼
LoggingBehavior (start timer)
        │
        ▼
EventStoreCommandBehavior (pre-handler — nothing yet)
        │
        ▼
GenerateGroupCommandHandler
  ├── teamsRepository.GetAllAsync()              → loads all Team aggregates from KurrentDB
  ├── random subset of size capacity selected
  ├── Group.Create(id, name, capacity)           → validates; enqueues GroupCreated
  ├── group.AddTeam(TeamInfo.FromTeam(t)) x N   → enqueues TeamAdded per team
  ├── fixtureScheduler.GenerateFixtures(group)  → Round Robin; enqueues MatchScheduled per match
  └── groupsRepository.Add(group)               → tracks aggregate in IEventStoreSession
        │
        ▼
EventStoreCommandBehavior (post-handler)
  └── IEventStoreSession.CommitAsync()
        └── appends GroupCreated + TeamAdded(N) + MatchScheduled(N) to KurrentDB stream group-{id}
        │
        ▼
KurrentDbForwarderService (EventRelay WebJob)
  └── maps eligible domain events to integration events and publishes to Kafka
        │
        ▼
LoggingBehavior (log elapsed time)
        │
        ▼
Returns Result<Guid> (GroupId) → 200 OK
```

### Write — Simulate Group

```
POST /api/v1/groups/{id}/simulate
        │
        ▼
SimulateGroupCommandHandler
  ├── repository.FindAsync(groupId)              → replays group-{id} stream from KurrentDB
  └── groupSimulator.SimulateAllMatches(group)
        ├── For each unplayed Match:
        │     ├── matchSimulator.SimulateMatch(home, away)  ← Poisson distribution
        │     └── group.SimulateMatch(matchId, homeScore, awayScore)
        │           └── match.SimulateResult(...)
        │                 └── Enqueue(new MatchPlayed(...))
        │
        ▼
EventStoreCommandBehavior
  └── IEventStoreSession.CommitAsync()
        └── appends MatchPlayed events to KurrentDB stream group-{id}
        │
        ▼
KurrentDbForwarderService (EventRelay WebJob)
  └── maps MatchPlayed → MatchPlayedIntegrationEvent → publishes to simulator.group
        │
        ▼
KafkaConsumerHost  (WebJob — BackgroundService)
  ├── receives message from simulator.group topic
  ├── IInboundPipeline.ProcessAsync(envelope, context)
  │     ├── TracingMiddleware   → starts OTel span
  │     ├── LoggingMiddleware   → logs receipt
  │     └── RetryMiddleware     → wraps handler with exponential back-off
  │           └── ProjectionMessageHandler<MatchPlayedIntegrationEvent>
  │                 ├── idempotency check: ProcessedEvents table
  │                 ├── IProjectionDispatcher.DispatchAsync(matchPlayedIntegrationEvent)
  │                 │     ├── MatchResultProjection      [priority 1] → inserts MatchResultModel row
  │                 │     └── GroupStandingsProjection   [priority 2] → updates stats + recalculates positions
  │                 ├── processedEventsRepository.Add(eventId, consumerGroupId)
  │                 └── unitOfWork.CommitAsync()
  └── consumer.Commit(consumeResult)   → manual Kafka offset commit
```

### Read — Get Standings

```
GET /api/v1/groups/{id}/standings
        │
        ▼
GroupStandingsQueryHandler
  ├── standingsRepository.GetStandingsByGroupIdAsync(groupId)   → read DB
  └── matchResultRepository.GetMatchResultsByGroupIdAsync(groupId) → read DB
        │
        ▼
Maps to GroupStandingsDto → 200 OK
```

---

## Dependency Graph (simplified)

```
Api
 ├── Application.Commands
 │    └── Simulator.Domain
 │         └── Core.Domain ← Core
 ├── Application.Queries
 │    └── Simulator.ReadModels
 ├── Core.EventSourcing.EventStoreDB
 │    └── Core.EventSourcing
 ├── Core.Messaging.Kafka
 │    ├── Core.Messaging.Pipeline
 │    └── Core.Messaging
 ├── Simulator.IntegrationEvents        (MatchPlayedIntegrationEvent, MatchPlayedIntegrationEventMapper)
 ├── Core.ServiceDefaults        (Serilog)
 └── Infrastructure.Read

ReadModels.WebJob
 ├── Simulator.ReadModels.Projections
 │    └── Simulator.ReadModels
 ├── Core.Messaging.Kafka
 │    ├── Core.Messaging.Pipeline
 │    └── Core.Messaging
 ├── Core.ServiceDefaults        (Serilog)
 └── Infrastructure.Read

EventRelay.WebJob
 ├── Core.EventSourcing.EventStoreDB
 ├── Core.Application             (integration event mappers registry)
 ├── Core.Messaging.Kafka
 │    ├── Core.Messaging.Pipeline
 │    └── Core.Messaging
 ├── Simulator.IntegrationEvents
 └── Core.ServiceDefaults         (Serilog)
```

---

