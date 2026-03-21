# Copilot Instructions

## Agent Guidelines
- Correct the user's English when responding to their requests, especially after discussing code changes.

---

# Miniclip Simulator — Project Context

> This is a mirror of [`AI.md`](../AI.md), which is the canonical source of truth.
> Update `AI.md` first; then sync this file.

---

## Project Overview

**Miniclip Simulator** is a football group-stage simulator REST API.
It allows clients to generate a group with random teams, simulate all matches in the group, and query the final standings with results.

The solution lives under `src/` and is orchestrated by **.NET Aspire** using **EventStoreDB** as the write store, **MySQL** as the read store, and **Kafka** as the distributed event bus.
The stack targets **.NET 10**.

---

## Architecture

The solution follows **Clean Architecture** combined with **CQRS**, **DDD**, **Event Sourcing**, and an **event-driven projection** model.

```
┌──────────────┐  Commands/Queries  ┌───────────────────────────────────────────┐
│  REST API    │ ──────────────────▶│  Mediator Pipeline                        │
│  (v1)        │                    │  ├─ EventStoreCommandBehavior (commit)    │
└──────────────┘                    │  └─ DomainEventPublisherBehavior (publish)│
                                    └────────────┬──────────────────────────────┘
                                                 │
                     ┌───────────────────────────┼──────────────────────┐
                     ▼                           ▼                      ▼
           ┌──────────────────┐       ┌─────────────────┐    ┌──────────────────┐
           │   EventStoreDB   │       │     Kafka       │    │   Read DB        │
           │  (write / source │       │   (event bus)   │    │   (MySQL)        │
           │   of truth)      │       └────────┬────────┘    └────────┬─────────┘
           │  · group-{id}    │                │                      │
           │  · team-{id}     │                ▼                      │
           └──────────────────┘       ┌───────────────────┐           │
                                      │ProjectionsConsumer│───────────▶
                                      │ (per event type)  │
                                      └───────────────────┘
```

### Write-Side Pipeline (Mediator)

Registration order (outermost first):
1. `DomainEventPublisherBehavior` — publishes committed events via `IEventBus` (Kafka) after EventStoreDB commit
2. `EventStoreCommandBehavior` — innermost; calls `IEventStoreSession.CommitAsync()` after the handler succeeds

> `ReadModelUnitOfWorkBehavior` was removed in Phase 4 — the read side is now updated exclusively by Kafka consumers.

---

## Project Structure

| Project | Layer | Responsibility |
|---|---|---|
| `Miniclip.Core` | Shared Kernel | `Result<T>`, `ExceptionBase`, string/enumerable extensions |
| `Miniclip.Core.Domain` | Domain Abstractions | `AggregateRoot`, `IAggregateRepository<T>`, `IDomainEvent` |
| `Miniclip.Core.Application` | Application Abstractions | `IEventBus`, pipeline behaviour base types |
| `Miniclip.Core.ReadModels` | Read Abstractions | `IReadModelUnitOfWork`, projection handler base types |
| `Miniclip.Core.ReadModels.Projections` | Projection Infrastructure | `[HandlerPriority]` attribute, ordered projection execution |
| `Miniclip.Core.EF` | EF Infrastructure | Generic EF Core base context |
| `Miniclip.Core.EventSourcing` | Event Sourcing Abstractions | `IEventStore<T>`, `IEventStoreSession`, `AggregateRepository<T>` |
| `Miniclip.Core.EventSourcing.EventStoreDB` | Event Sourcing Infrastructure | `EventStoreDbEventStore<T>`, `EventStoreSession`, `SystemTextJsonEventSerializer` |
| `Miniclip.Core.Kafka` | Kafka Infrastructure | `KafkaConsumerService`, `KafkaEventBus`, `TopicNaming`, retry/DLQ policy |
| `Miniclip.Simulator.Domain` | Domain | `Group`, `Team` aggregates, domain services, value objects |
| `Miniclip.Simulator.Application.Commands` | Application – Write | `GenerateGroupCommand`, `SimulateGroupCommand` handlers |
| `Miniclip.Simulator.Application.Queries` | Application – Read | `GroupStandingsQuery` handler |
| `Miniclip.Simulator.ReadModels` | Read Models | `GroupStandingsModel`, `MatchResultModel`, repository interfaces |
| `Miniclip.Simulator.ReadModels.Projections` | Projections | `ProjectionsConsumerService<TEvent>` |
| `Miniclip.Simulator.Infrastructure.Read` | Infrastructure – Read | `SimulatorReadDbContext`, repository implementations |
| `Miniclip.Simulator.Infrastructure.Write` | Infrastructure – Write | EF migrations only (all aggregate tables dropped; empty model) |
| `Miniclip.Simulator.Api` | API | `GroupsController`, configuration wiring, `TeamDataSeeder` |
| `Miniclip.Simulator.AppHost` | Orchestration | .NET Aspire AppHost; provisions MySQL, EventStoreDB, Kafka |

---

## Key Domain Concepts

- **Group** — The core write-side aggregate. Stored as an event stream in EventStoreDB (`group-{id}`). Owns a list of `TeamInfo` value object snapshots and `Match` entities. Capacity 2–6. Emits: `GroupCreated`, `TeamAdded`, `MatchScheduled`, `MatchPlayed`.
- **TeamInfo** — Value object `(Guid Id, string Name, int Strength)` captured at group creation. `Group` and `Match` use `TeamInfo` instead of `Team` references to respect aggregate boundaries.
- **Team** — An event-sourced aggregate stored in EventStoreDB (`team-{id}`). Emits `TeamRegistered`. A fixed squad of 10 teams is seeded at startup by `TeamDataSeeder`. Strength (0–100) influences match outcomes.
- **Match** — An entity owned by `Group`. Has `TeamInfo HomeTeam`, `TeamInfo AwayTeam`, `Round`, and scores. Can only be simulated once (`IsPlayed`).
- **Fixture Scheduling** — Uses a **Round Robin** algorithm. Odd team counts add a dummy bye slot (internal to the scheduler, not a `Team` aggregate).
- **Match Simulation** — Uses a **Poisson distribution** based on each team's `Strength`. Home team gets a `1.1×` advantage multiplier.
- **MatchPlayed** — The domain event that drives all read-model updates. Published to Kafka after being committed to EventStoreDB.
- **GroupStandings** — A read model tracking Points, Wins, Draws, Losses, GF/GA, GD, and Position per team. Rebuilt from `MatchPlayed` Kafka events.

---

## Domain Events

| Event | Aggregate | Published to Kafka |
|---|---|---|
| `TeamRegistered` | `Team` | No (seeder only; not consumed by projections) |
| `GroupCreated` | `Group` | No (internal; group state is loaded from ESDB) |
| `TeamAdded` | `Group` | No |
| `MatchScheduled` | `Group` | No |
| `MatchPlayed` | `Group` | **Yes** → drives `GroupStandingsProjection` + `MatchResultProjection` |

---

## Patterns & Conventions

### Result Pattern
All operations return `Result` or `Result<T>` — **never throw exceptions for business rule violations**.

### CQRS
- **Commands** modify state and live in `Miniclip.Simulator.Application.Commands`. They use `IAggregateRepository<T>`.
- **Queries** read from denormalised read models in `Miniclip.Simulator.Application.Queries`.

### Domain Events
- Aggregates enqueue events via `Enqueue(IDomainEvent)` (from `AggregateRoot`) AND set their state directly in the constructor/factory (so the aggregate is immediately usable after `Create`).
- `Apply(IDomainEvent)` handles replay from EventStoreDB only — it is not called during normal command processing.
- Events are committed to **EventStoreDB** by `EventStoreCommandBehavior`, then published to **Kafka** by `DomainEventPublisherBehavior`.
- `ProjectionsConsumerService<TEvent>` consumes each topic, creates a **fresh DI scope per message** via `IServiceScopeFactory` (avoiding captive dependency issues with scoped `DbContext`), and dispatches to ordered `INotificationHandler<TEvent>` handlers.
- Idempotency: the `ProcessedEvents` table records each `event-id` + consumer group ID before committing.

### Kafka Consumer Lifecycle
`KafkaConsumerService` (abstract `BackgroundService`) builds its own `IConsumer<string,byte[]>` via the abstract `BuildConsumer(ConsumerConfig)` method, using `ConsumerGroupId` from the subclass. This keeps Kafka configuration out of the DI container and avoids the captive dependency problem.

### Mediator
Uses the **Mediator** NuGet package (source-generated — **not MediatR**). Commands/queries implement `IRequest<TResponse>`; handlers implement `IRequestHandler<TRequest, TResponse>`.

### Versioning
API is versioned with `Asp.Versioning`. All routes follow `api/v{version}/[controller]`. Current version: `v1`.

### Error Mapping
`ResultExtensions.ToActionResult()` maps `Result` failures to HTTP status codes (400 / 404 / 204).

### EF Core
Only the **read side** uses EF Core (`SimulatorReadDbContext`). The write `DbContext` (`SimulatorWriteDbContext`) has an empty model — it exists only to run the migration that dropped all legacy aggregate tables. Both contexts are migrated at startup via `app.InitializeDatabases()`.

---

## Testing

| Project | What it tests |
|---|---|
| `Miniclip.Simulator.Domain.UnitTests` | Aggregate logic, fixture scheduling, simulation |
| `Miniclip.Simulator.Application.Commands.UnitTests` | Command handler logic |
| `Miniclip.Simulator.Application.Queries.UnitTests` | Query handler logic |
| `Miniclip.Simulator.ReadModels.Projections.UnitTests` | `ProjectionsConsumerService` idempotency; projection handlers |
| `Miniclip.Simulator.ReadModels.Projections.IntegrationTests` | Full projection pipeline against a real read DB |
| `Miniclip.Core.Kafka.UnitTests` | `KafkaConsumerService` retry policy and DLQ routing |
| `Miniclip.Simulator.Api.UnitTests` | Controller / result extension behaviour |
| `Miniclip.Simulator.Common.Tests` | Shared test helpers and builders |
| `Miniclip.Core.Tests` | Shared kernel tests |

---

## Further Reading

- [`docs/architecture.md`](../docs/architecture.md) — Layer responsibilities, full request flow, dependency graph
- [`docs/domain-model.md`](../docs/domain-model.md) — Aggregates, business rules, simulation algorithm, read model schema
- [`docs/adr/`](../docs/adr/) — Architecture Decision Records
- [`docs/event-sourcing/PLAN.md`](../docs/event-sourcing/PLAN.md) — Event Sourcing migration phases (all complete)

---

## Running Locally

```bash
cd src/Miniclip.Simulator.AppHost
dotnet user-secrets set "Parameters:mysql-password" "<your-password>"
dotnet run
```

Aspire provisions MySQL, EventStoreDB, Kafka, and the API. Migrations and team seeding run automatically.


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
│  Infrastructure     │  │  Infrastructure             │
│  .Write (EF, UoW)   │  │  .Read  (EF, read repos)   │
└────────────────────┘  └────────────────────────────┘
```

The **Write** side persists aggregate state via EF Core.  
The **Read** side is populated exclusively through **domain event projections** (`MatchPlayed`).

---

## Project Structure

| Project | Layer | Responsibility |
|---|---|---|
| `Miniclip.Core` | Shared Kernel | `Result<T>`, `ExceptionBase`, string/enumerable extensions |
| `Miniclip.Core.Domain` | Domain Abstractions | `AggregateRoot`, `IRepository<T>`, `IUnitOfWork`, `IDomainEvent` |
| `Miniclip.Core.Application` | Application Abstractions | Mediator pipeline wiring, shared handlers contracts |
| `Miniclip.Core.ReadModels` | Read Abstractions | Read model base types and repository interfaces |
| `Miniclip.Core.ReadModels.Projections` | Projection Infrastructure | `[HandlerPriority]` attribute, ordered projection execution |
| `Miniclip.Core.EF` | EF Infrastructure | Generic EF Core base context and repository |
| `Miniclip.Core.EventSourcing` | Event Sourcing Abstractions | `IEventStore<T>`, `IEventStoreSession`, `IEventSerializer`, `EventEnvelope`, `EventSourcedRepository<T>` |
| `Miniclip.Core.EventSourcing.EventStoreDB` | Event Sourcing Infrastructure | EventStoreDB client, `EventStoreDbEventStore<T>`, `EventStoreSession`, `SystemTextJsonEventSerializer` |
| `Miniclip.Core.Kafka` | Kafka Infrastructure | `KafkaEventBus`, `KafkaConsumerService` base, `TopicNaming`, `ServiceCollectionExtensions` |
| `Miniclip.Core.ServiceDefaults` | Aspire Defaults | Shared .NET Aspire service defaults |
| `Miniclip.Simulator.Domain` | Domain | `Group`, `Team`, `Match` aggregates, fixture scheduling, match simulation |
| `Miniclip.Simulator.Application.Commands` | Application – Write | `GenerateGroupCommand`, `SimulateGroupCommand` handlers |
| `Miniclip.Simulator.Application.Queries` | Application – Read | `GroupStandingsQuery` handler |
| `Miniclip.Simulator.ReadModels` | Read Models | `GroupStandingsModel`, `MatchResultModel` |
| `Miniclip.Simulator.ReadModels.Projections` | Projections | `GroupStandingsProjection`, `MatchResultProjection` |
| `Miniclip.Simulator.Infrastructure.Write` | Infrastructure – Write | `SimulatorWriteDbContext`, `GroupsRepository`, `SimulatorUnitOfWork` |
| `Miniclip.Simulator.Infrastructure.Read` | Infrastructure – Read | `SimulatorReadDbContext`, read/write repos for read models |
| `Miniclip.Simulator.Api` | API | `GroupsController`, configuration wiring, `Startup` |
| `Miniclip.Simulator.AppHost` | Orchestration | .NET Aspire AppHost, MySQL provisioning |

---

## Key Domain Concepts

- **Group** – The core aggregate. Holds a list of `Team`s and `Match`es. Capacity is 2–6 teams. A group must be fully generated before it can be simulated.
- **Team** – An aggregate with a `Strength` value (0–100) that influences match outcomes.
- **Match** – An entity owned by `Group`. Has `HomeTeam`, `AwayTeam`, `Round`, and scores. Can only be simulated once (`IsPlayed`).
- **Fixture Scheduling** – Uses a **Round Robin** algorithm. Odd team counts add a `Team.Dummy` bye slot. Home/away balance is tracked via counters.
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
- **Queries** read from the denormalized read models and live in `Miniclip.Simulator.Application.Queries`. They use read-specific repository interfaces.

### Domain Events
- Aggregates enqueue events via `Enqueue(IDomainEvent)` (inherited from `AggregateRoot`).
- Events are dequeued and dispatched via `IEventBus` (Kafka) by `DomainEventPublisherBehavior` after EventStoreDB commit.
- A `MatchPlayedKafkaRelayService` (`BackgroundService`) consumes `simulator.match-played` from Kafka and re-publishes via Mediator so existing projection handlers work unchanged (temporary bridge, removed in Phase 4).
- Projections implement `INotificationHandler<TEvent>` and are decorated with `[HandlerPriority(n)]` to control execution order.

### Mediator
Uses the **Mediator** NuGet package (source-generated, not MediatR). Commands and queries implement `IRequest<TResponse>`, handlers implement `IRequestHandler<TRequest, TResponse>`.

### Versioning
API is versioned with `Asp.Versioning`. All routes follow `api/v{version}/[controller]`. Current version: `v1`.

### Error Mapping
The `ResultExtensions.ToActionResult()` extension in the API layer maps `Result` failures to the appropriate HTTP status codes (400 / 404 / 204).

### EF Core
- Two separate `DbContext`s: `SimulatorWriteDbContext` (write) and `SimulatorReadDbContext` (read).
- Both are seeded/migrated at startup via `app.InitializeDatabases()`.
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

## Running Locally

The entry point for local development is the **AppHost**:
```
Miniclip.Simulator.AppHost
```
It provisions a MySQL container and starts the API via .NET Aspire.

---

## Active Migration

The project is currently undergoing an **Event Sourcing migration** using EventStoreDB and Kafka.

**Current Phase:** `5 — Testing & Hardening` ⬜

| # | Phase | Status |
|---|---|---|
| 0 | Planning & Documentation | ✅ Done |
| 1 | EventStoreDB — Core Abstractions | ✅ Done |
| 2 | EventStoreDB — Write Side Migration | ✅ Done |
| 3 | Kafka — Event Bus | ✅ Done |
| 4 | Kafka — Read Side Consumers | ✅ Done |
| 5 | Testing & Hardening | ⬜ Pending |

Full plan and per-phase specs: [`docs/event-sourcing/PLAN.md`](../docs/event-sourcing/PLAN.md)

> Before working on any migration task, read `PLAN.md` and confirm the current phase status above.
