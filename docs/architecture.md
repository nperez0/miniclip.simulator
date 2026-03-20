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
| `AggregateRoot` | Base class for all DDD aggregates. Owns the domain event queue (`Enqueue` / `DequeueUncommittedEvents`). |
| `IDomainEvent` | Marker interface implemented by all domain events. |
| `IRepository<T>` | Generic write-side repository interface. |
| `IUnitOfWork` | Abstracts transaction commit + domain event dispatching. |

### Domain — `Miniclip.Simulator.Domain`

Pure business logic. No dependencies on ASP.NET Core, EF Core, or any infrastructure library.

```
Aggregates/
├── Groups/
│   ├── Entities/         Group.cs, Match.cs
│   ├── Events/           MatchPlayed.cs
│   ├── Exceptions/       GroupCreationException, GroupAddTeamException, ...
│   └── Services/
│       ├── Fixtures/     IFixtureScheduler, RoundRobinScheduler, FixtureSchedulerService
│       └── Simulator/    IMatchSimulator, MatchSimulator, GroupSimulator
└── Teams/
    ├── Entities/         Team.cs
    └── Exceptions/       TeamCreationException.cs
```

### Application — Commands & Queries

Split into two projects to enforce CQRS.

**`Miniclip.Simulator.Application.Commands`** (write side)
- Depends on `Miniclip.Simulator.Domain` and `IRepository<T>`.
- Handlers: `GenerateGroupCommandHandler`, `SimulateGroupCommandHandler`.
- Uses `IUnitOfWork` via the infrastructure layer to commit + dispatch events.

**`Miniclip.Simulator.Application.Queries`** (read side)
- Depends only on read model repository interfaces (`IGroupStandingsRepository`, `IMatchResultsRepository`).
- Handlers: `GroupStandingsQueryHandler`.
- Never touches the write `DbContext` or domain aggregates.

### Read Models & Projections

**`Miniclip.Simulator.ReadModels`** — POCO read model definitions (`GroupStandingsModel`, `MatchResultModel`).  
**`Miniclip.Simulator.ReadModels.Projections`** — `INotificationHandler<MatchPlayed>` implementations.

Projections are the **only** writers to the read database. They run in priority order via `[HandlerPriority(n)]`:

| Priority | Projection | What it does |
|---|---|---|
| 1 | `MatchResultProjection` | Creates a `MatchResultModel` row for the played match. |
| 2 | `GroupStandingsProjection` | Gets or creates standings rows for both teams, updates all stats, then recalculates positions. |

### Infrastructure — Write & Read

Two separate `DbContext`s, each with their own MySQL connection:

| Context | Project | Tracks |
|---|---|---|
| `SimulatorWriteDbContext` | `Infrastructure.Write` | `Group`, `Team`, `Match` aggregates |
| `SimulatorReadDbContext` | `Infrastructure.Read` | `GroupStandingsModel`, `MatchResultModel` |

The read DB context exposes both read (query) and write (projection) repositories under `Persistence/Repositories/Read/` and `Persistence/Repositories/Write/`.

### API — `Miniclip.Simulator.Api`

ASP.NET Core Web API. Versioned using `Asp.Versioning`.

```
Controllers/V1/    GroupsController
Extensions/        ResultExtensions  (Result<T> → IActionResult mapping)
Infrastructure/
  Configuration/   DatabaseConfiguration, DomainConfiguration,
                   MediatorConfiguration, ProjectionsConfiguration,
                   ApiVersioningConfiguration
Startup.cs         ConfigureServices + Configure
Program.cs         Host builder entry point
```

### Orchestration — `Miniclip.Simulator.AppHost`

.NET Aspire AppHost. Provisions:
- A **MySQL** container.
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
  ├── Group.Create(id, name, capacity)          → validates and creates aggregate
  ├── GetRandomTeams(capacity)                  → fetches random teams from write DB
  ├── group.AddTeam(team) × N                   → business rule: max capacity, no duplicates
  ├── fixtureSchedulerService.GenerateFixtures  → Round Robin scheduling, adds Matches to Group
  └── groupsRepository.Add(group)
        │
        ▼
IUnitOfWork.CommitAsync()
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
  ├── repository.FindAsync(groupId)              → loads Group aggregate
  └── groupSimulator.SimulateAllMatches(group)
        ├── For each unplayed Match:
        │     ├── matchSimulator.SimulateMatch(homeStrength, awayStrength)   ← Poisson
        │     └── group.SimulateMatch(matchId, homeScore, awayScore)
        │           └── match.SimulateResult(...)
        │                 └── Enqueue(new MatchPlayed(...))
        │
        ▼
IUnitOfWork.CommitAsync()
  ├── Saves updated Match scores to write DB
  └── DequeueUncommittedEvents()
        └── For each MatchPlayed event → dispatched as INotification
              ├── MatchResultProjection.Handle(...)     [priority 1]
              └── GroupStandingsProjection.Handle(...)  [priority 2]
                    ├── Updates stats (W/D/L, GF/GA, Points)
                    └── RecalculatePositionService.RecalculatePositionsAsync(...)
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
 ├── Infrastructure.Write
 │    └── Core.EF
 └── Infrastructure.Read
      └── Core.EF
```
