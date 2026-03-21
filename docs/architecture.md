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
- `GenerateGroupCommandHandler` loads all teams from EventStoreDB via `IAggregateRepository<Team>.GetAllAsync()`, picks a random subset, and creates a `Group`.

**`Miniclip.Simulator.Application.Queries`** (read side)
- Depends only on read model repository interfaces (`IGroupStandingsRepository`, `IMatchResultsRepository`).
- Handlers: `GroupStandingsQueryHandler`.
- Never touches the write `DbContext` or domain aggregates.

### Read Models & Projections

**`Miniclip.Simulator.ReadModels`** — POCO read model definitions (`GroupStandingsModel`, `MatchResultModel`) and repository interfaces.

**`Miniclip.Simulator.ReadModels.Projections`** — `ProjectionsConsumerService<TEvent>` (Kafka consumer) and `INotificationHandler<TEvent>` projection handlers dispatched in priority order via Mediator.

Projections are the **only** writers to the read database. They run in priority order via `[HandlerPriority(n)]`:

| Priority | Projection | What it does |
|---|---|---|
| 1 | `MatchResultProjection` | Creates a `MatchResultModel` row for the played match. |
| 2 | `GroupStandingsProjection` | Gets or creates standings rows for both teams, updates all stats, then recalculates positions. |

### Infrastructure — Write & Read

The **write side** is EventStoreDB. `SimulatorWriteDbContext` has an empty EF model and exists only to run the migration that dropped all legacy aggregate tables.

`SimulatorReadDbContext` holds all read models and exposes both read and write repositories:

| Context | Project | Tracks |
|---|---|---|
| `SimulatorWriteDbContext` | `Infrastructure.Write` | Nothing (empty model; migrations only) |
| `SimulatorReadDbContext` | `Infrastructure.Read` | `GroupStandingsModel`, `MatchResultModel`, `ProcessedEventsModel` |

The read DB context exposes both read (query) and write (projection) repositories under `Persistence/Repositories/Read/` and `Persistence/Repositories/Write/`.

### API — `Miniclip.Simulator.Api`

ASP.NET Core Web API. Versioned using `Asp.Versioning`.

```
Controllers/V1/    GroupsController
Extensions/        ResultExtensions  (Result<T> → IActionResult mapping)
Infrastructure/
  Configuration/   EventStoreDbConfiguration, ReadModelsConfiguration,
                   KafkaConfiguration, MediatorConfiguration,
                   DomainConfiguration, ApiVersioningConfiguration
  Seeding/         TeamDataSeeder  (seeds 10 teams to EventStoreDB on startup)
Startup.cs         ConfigureServices + Configure
Program.cs         Host builder entry point
```

### Orchestration — `Miniclip.Simulator.AppHost`

.NET Aspire AppHost. Provisions:
- A **MySQL** container (write migrations DB + read model DB).
- An **EventStoreDB** container (with `$by_category` and standard projections enabled).
- A **Kafka** container with **Kafka UI**.
- The **API** project as a service.

Entry point for local development.

---

## Request Flow

### Write — Generate Group

```
POST /api/v1/groups
        │
        ▼
GenerateGroupCommandHandler
  ├── teamsRepository.GetAllAsync()              → loads all Team aggregates from EventStoreDB
  ├── random subset of size capacity selected
  ├── Group.Create(id, name, capacity)           → validates; enqueues GroupCreated
  ├── group.AddTeam(TeamInfo.FromTeam(t)) x N   → enqueues TeamAdded per team
  ├── fixtureScheduler.GenerateFixtures(group)  → Round Robin; enqueues MatchScheduled per match
  └── groupsRepository.Add(group)               → tracks aggregate in IEventStoreSession
        │
        ▼
EventStoreCommandBehavior → IEventStoreSession.CommitAsync()
  └── appends GroupCreated + TeamAdded(N) + MatchScheduled(N) to ESDB stream group-{id}
        │
        ▼
DomainEventPublisherBehavior → IEventBus.PublishAsync() per committed event
  └── only MatchPlayed is consumed by projections; Group creation events are ESDB-only
        │
        ▼
Returns Result<Guid> (GroupId) → 200 OK
```IUnitOfWork.CommitAsync()
  ├── Saves Group + Teams + Matches to write DB
  └── DequeueUncommittedEvents() → dispatches nothing yet (no match played)
        │
        ▼
Returns Result<Guid> (GroupId) → 204 No Content
```

### Write — Simulate Group

```
POST /api/v1/groups/{id}/simulate
        │
        ▼
SimulateGroupCommandHandler
  ├── repository.FindAsync(groupId)              → replays group-{id} stream from EventStoreDB
  └── groupSimulator.SimulateAllMatches(group)
        ├── For each unplayed Match:
        │     ├── matchSimulator.SimulateMatch(home, away)  ← Poisson distribution
        │     └── group.SimulateMatch(matchId, homeScore, awayScore)
        │           └── match.SimulateResult(...)
        │                 └── Enqueue(new MatchPlayed(...))
        │
        ▼
EventStoreCommandBehavior → IEventStoreSession.CommitAsync()
  └── appends MatchPlayed events to ESDB stream group-{id}
        │
        ▼
DomainEventPublisherBehavior → IEventBus.PublishAsync() per MatchPlayed
  └── publishes to simulator.match-played Kafka topic
        │
        ▼
ProjectionsConsumerService<MatchPlayed>  (background service)
  ├── Deduplication: ProcessedEvents table checked before processing
  ├── IPublisher.Publish(matchPlayed) dispatches to ordered handlers:
  │     ├── MatchResultProjection      [priority 1] → inserts MatchResultModel row
  │     └── GroupStandingsProjection   [priority 2] → updates stats + recalculates positions
  └── ProcessedEvents row written; transaction committed
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
 │    └── Core.ReadModels
 ├── ReadModels.Projections
 │    ├── Simulator.Domain (MatchPlayed event)
 │    └── Simulator.ReadModels
 ├── Core.EventSourcing.EventStoreDB
 │    └── Core.EventSourcing
 ├── Core.Kafka
 │    └── Core.Application
 └── Infrastructure.Read
      └── Core.EF
```
