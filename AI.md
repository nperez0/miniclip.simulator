# Miniclip Simulator — Project Context

> This is the **canonical AI context file** for this repository.
> It is tool-agnostic and kept as the single source of truth.
> Tool-specific files (`.github/copilot-instructions.md`, `AGENTS.md`, `CLAUDE.md`) mirror or reference this file.

---

## Project Overview

**Miniclip Simulator** is a football group-stage simulator REST API.
It allows clients to generate a group with random teams, simulate all matches in the group, and query the final standings with results.

The solution lives under `src/` and is orchestrated by **.NET Aspire** using **MySQL** as the database engine.
The stack targets **.NET 10**.

---

## Architecture

The solution follows **Clean Architecture** combined with **CQRS**, **DDD**, and an **event-driven projection** model.

```
┌─────────────────────────────────────────────┐
│               Miniclip.Simulator.Api         │  ← ASP.NET Core, versioned REST API
└──────────────────────┬──────────────────────┘
                       │ Mediator (commands / queries)
          ┌────────────┴─────────────┐
          │                          │
┌─────────▼──────────┐  ┌───────────▼────────────────┐
│  Application        │  │  Application                │
│  .Commands          │  │  .Queries                   │
│  (write side)       │  │  (read side)                │
└─────────┬──────────┘  └───────────┬────────────────┘
          │                          │
┌─────────▼──────────┐  ┌───────────▼────────────────┐
│  Simulator.Domain   │  │  ReadModels +               │
│  (DDD aggregates)   │  │  ReadModels.Projections     │
└─────────┬──────────┘  └───────────┬────────────────┘
          │ Domain Events            │ INotificationHandler<MatchPlayed>
┌─────────▼──────────┐  ┌───────────▼────────────────┐
│  EventStoreDB       │  │  Infrastructure             │
│  (event streams)    │  │  .Read  (EF, read repos)   │
└────────────────────┘  └────────────────────────────┘
```

The **Write** side persists domain events to **EventStoreDB** (replaced EF Core in Phase 2).
The **Read** side is still on MySQL, populated through `MatchPlayed` domain event projections.

### Write-Side Pipeline (Mediator)

Registration order (first = outermost):
1. `ReadModelUnitOfWorkBehavior` — wraps the full pipeline; saves the read model (MySQL) on success
2. `DomainEventPublisherBehavior` — publishes committed events after EventStoreDB commit
3. `EventStoreCommandBehavior` — innermost; calls `IEventStoreSession.CommitAsync()` after the handler succeeds

---

## Project Structure

| Project | Layer | Responsibility |
|---|---|---|
| `Miniclip.Core` | Shared Kernel | `Result<T>`, `ExceptionBase`, string/enumerable extensions |
| `Miniclip.Core.Domain` | Domain Abstractions | `AggregateRoot`, `IRepository<T>`, `IDomainEvent` |
| `Miniclip.Core.Application` | Application Abstractions | Mediator pipeline wiring, shared handler contracts |
| `Miniclip.Core.ReadModels` | Read Abstractions | Read model base types and repository interfaces |
| `Miniclip.Core.ReadModels.Projections` | Projection Infrastructure | `[HandlerPriority]` attribute, ordered projection execution |
| `Miniclip.Core.EF` | EF Infrastructure | Generic EF Core base context and repository |
| `Miniclip.Core.EventSourcing` | Event Sourcing Abstractions | `IEventStore<T>`, `IEventStoreSession`, `IEventSerializer`, `EventEnvelope`, `EventSourcedRepository<T>` |
| `Miniclip.Core.EventSourcing.EventStoreDB` | Event Sourcing Infrastructure | EventStoreDB client, `EventStoreDbEventStore<T>`, `EventStoreSession`, `SystemTextJsonEventSerializer` |
| `Miniclip.Simulator.Domain` | Domain | `Group`, `Team`, `Match` aggregates, fixture scheduling, match simulation |
| `Miniclip.Simulator.Application.Commands` | Application – Write | `GenerateGroupCommand`, `SimulateGroupCommand` handlers |
| `Miniclip.Simulator.Application.Queries` | Application – Read | `GroupStandingsQuery` handler |
| `Miniclip.Simulator.ReadModels` | Read Models | `GroupStandingsModel`, `MatchResultModel` |
| `Miniclip.Simulator.ReadModels.Projections` | Projections | `GroupStandingsProjection`, `MatchResultProjection` |
| `Miniclip.Simulator.Infrastructure.Write` | Infrastructure – Write | `SimulatorWriteDbContext` (Team reference data only), `TeamConfiguration` |
| `Miniclip.Simulator.Infrastructure.Read` | Infrastructure – Read | `SimulatorReadDbContext`, read/write repos for read models |
| `Miniclip.Simulator.Api` | API | `GroupsController`, configuration wiring, `Startup` |
| `Miniclip.Simulator.AppHost` | Orchestration | .NET Aspire AppHost, MySQL provisioning |

---

## Key Domain Concepts

- **Group** – The core write-side aggregate. Stored as an **event stream** in EventStoreDB. Holds a list of `TeamInfo` value object snapshots and `Match` entities. Capacity is 2–6 teams. A group must be fully generated before it can be simulated. Emits four events: `GroupCreated`, `TeamAdded`, `MatchScheduled`, `MatchPlayed`.
- **TeamInfo** – A value object snapshot `(Guid Id, string Name, int Strength)` captured at group creation time. `Group` and `Match` use `TeamInfo` instead of `Team` entity references to respect aggregate boundaries.
- **Team** – An aggregate with a `Strength` value (0–100) that influences match outcomes. Used as reference data at group creation; converted to `TeamInfo` snapshots immediately.
- **Match** – An entity owned by `Group`. Has `TeamInfo HomeTeam`, `TeamInfo AwayTeam`, `Round`, and scores. Can only be simulated once (`IsPlayed`).
- **Fixture Scheduling** – Uses a **Round Robin** algorithm. Odd team counts add a `TeamInfo.Dummy` bye slot. Home/away balance is tracked via counters.
- **Match Simulation** – Uses a **Poisson distribution** based on each team's `Strength`. Home team gets a `1.1x` advantage multiplier.
- **MatchPlayed** – The domain event fired after each match is simulated. Drives all read model updates.
- **GroupStandings** – A read model that tracks Points, Wins, Draws, Losses, Goals For/Against, Goal Difference, and Position per team. Position is recalculated after each `MatchPlayed` event.

---

## Patterns & Conventions

### Result Pattern
All operations return `Result` or `Result<T>` — **never throw exceptions for business rule violations**.
```csharp
// Correct
var result = Group.Create(id, name, capacity);
if (result.IsFailure) return Result.Failure<Guid>(result.Exception);

// Never
throw new Exception("Invalid capacity");
```

### CQRS
- **Commands** modify state and live in `Miniclip.Simulator.Application.Commands`. They use `IRepository<T>` (write side).
- **Queries** read from the denormalised read models and live in `Miniclip.Simulator.Application.Queries`. They use read-specific repository interfaces.

### Domain Events
- Aggregates enqueue events via `Enqueue(IDomainEvent)` (inherited from `AggregateRoot`).
- Events are dequeued and dispatched as `INotification` via **Mediator** after persistence.
- Projections implement `INotificationHandler<TEvent>` and are decorated with `[HandlerPriority(n)]` to control execution order.

### Mediator
Uses the **Mediator** NuGet package (source-generated, **not MediatR**). Commands and queries implement `IRequest<TResponse>`, handlers implement `IRequestHandler<TRequest, TResponse>`.

### Versioning
API is versioned with `Asp.Versioning`. All routes follow `api/v{version}/[controller]`. Current version: `v1`.

### Error Mapping
The `ResultExtensions.ToActionResult()` extension in the API layer maps `Result` failures to the appropriate HTTP status codes (400 / 404 / 204).

### EF Core
- `SimulatorWriteDbContext` is kept only for **Team** reference data (read at group creation time).
- `SimulatorReadDbContext` holds all read models. Both contexts are migrated at startup via `app.InitializeDatabases()`.
- Entity configurations live in `Persistence/Configurations/`.

---

## Testing

| Project | What it tests |
|---|---|
| `Miniclip.Simulator.Domain.UnitTests` | Domain aggregate logic, fixture scheduling, simulation |
| `Miniclip.Simulator.Application.Commands.UnitTests` | Command handler logic |
| `Miniclip.Simulator.Application.Queries.UnitTests` | Query handler logic |
| `Miniclip.Simulator.ReadModels.Projections.UnitTests` | Projection handlers |
| `Miniclip.Simulator.Api.UnitTests` | Controller / result extension behaviour |
| `Miniclip.Simulator.Common.Tests` | Shared test helpers and builders |
| `Miniclip.Core.Tests` | Core shared kernel tests |

---

## Further Reading

- [`docs/architecture.md`](docs/architecture.md) — Layer responsibilities, full request flow diagrams, dependency graph
- [`docs/domain-model.md`](docs/domain-model.md) — Aggregates, business rules, simulation algorithm, read model schema
- [`docs/adr/`](docs/adr/) — Architecture Decision Records (why each key decision was made)

---

## Running Locally

The entry point for local development is the **AppHost**:
```
src/Miniclip.Simulator.AppHost
```
It provisions a MySQL container and starts the API via .NET Aspire.

---

## Active Migration

The project is currently undergoing an **Event Sourcing migration** using EventStoreDB and Kafka.

**Current Phase:** `2 — EventStoreDB: Write Side Migration` ✅

| # | Phase | Status |
|---|---|---|
| 0 | Planning & Documentation | ✅ Done |
| 1 | EventStoreDB — Core Abstractions | ✅ Done |
| 2 | EventStoreDB — Write Side Migration | ✅ Done |
| 3 | Kafka — Event Bus | ⬜ Pending |
| 4 | Kafka — Read Side Consumers | ⬜ Pending |
| 5 | Testing & Hardening | ⬜ Pending |

Full plan and per-phase specs: [`docs/event-sourcing/PLAN.md`](docs/event-sourcing/PLAN.md)

> **For AI agents:** Before working on any migration task, read `PLAN.md` and confirm the current phase status above. Update the phase status in this file when a phase is completed.
