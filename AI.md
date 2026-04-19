# Miniclip Simulator — Project Context

> This is the **canonical AI context file** for this repository.
> It is tool-agnostic and kept as the single source of truth.
> Tool-specific files (`.github/copilot-instructions.md`, `AGENTS.md`, `CLAUDE.md`) mirror or reference this file.

---

## Project Overview

**Miniclip Simulator** is a football group-stage simulator REST API.
It allows clients to generate a group with random teams, simulate all matches in the group, and query the final standings with results.

The solution lives under `src/` and is orchestrated by **.NET Aspire** using **KurrentDB** as the write store, **MySQL** as the read store, and **Kafka** as the distributed event bus.
The stack targets **.NET 10**.

> **KurrentDB** is the renamed EventStoreDB. The client library is `KurrentDB.Client`; the Docker image is `kurrentplatform/kurrentdb`. Concepts are identical: per-aggregate streams, optimistic concurrency, built-in `$by_category` projection.

---

## Architecture

The solution follows **Clean Architecture** combined with **CQRS**, **DDD**, **Event Sourcing**, and an **event-driven projection** model.

```
┌──────────────┐  Commands/Queries  ┌───────────────────────────────────────────────┐
│  REST API    │ ──────────────────▶│  Mediator Pipeline                            │
│  (v1)        │                    │  ├─ LoggingBehavior (timing + OTel tagging)  │
└──────────────┘                    │  └─ EventStoreCommandBehavior (commit+publish)│
                                    └────────────┬──────────────────────────────────┘
                                                 │
                     ┌───────────────────────────┼──────────────────────┐
                     ▼                           ▼                      ▼
           ┌──────────────────┐       ┌─────────────────┐    ┌──────────────────┐
           │   KurrentDB      │       │     Kafka       │    │   Read DB        │
           │  (write / source │       │   (event bus)   │    │   (MySQL)        │
           │   of truth)      │       └────────┬────────┘    └────────┬─────────┘
           │  · group-{id}    │                │                      │
           │  · team-{id}     │                ▼                      │
           └──────────────────┘       ┌────────────────────┐          │
                                      │ ReadModels WebJob  │          │
                                      │ ProjectionsConsumer│──────────▶
                                      │  (per aggregate)   │
                                      └────────────────────┘
```

### Write-Side Pipeline (Mediator)

Registration order (outermost first):
1. `LoggingBehavior` — logs request name, elapsed time, and domain errors; tags the active OTel `Activity` on conflicts.
2. `EventStoreCommandBehavior` — after the handler succeeds: calls `IEventStoreSession.CommitAsync()` then iterates `session.GetCommittedEvents()` and publishes each to `IEventBus` (Kafka).

> `ReadModelUnitOfWorkBehavior` was removed in Phase 4 — the read side is now updated exclusively by the ReadModels WebJob.

### ReadModels WebJob

`Miniclip.Simulator.ReadModels.WebJob` is a standalone **.NET Worker Service** that owns all Kafka projection consumers and the read-model database. It starts before the API in Aspire and is the **only** writer to the read DB.

---

## Project Structure

| Project | Layer | Responsibility |
|---|---|---|
| `Miniclip.Core` | Shared Kernel | `Result<T>`, `ExceptionBase`, string/enumerable extensions |
| `Miniclip.Core.Domain` | Domain Abstractions | `AggregateRoot`, `IAggregateRepository<T>`, `IDomainEvent` |
| `Miniclip.Core.Application` | Application Abstractions | `ICommand`, `IQuery`, pipeline behaviour base types (`LoggingBehavior`, `EventStoreCommandBehavior`), `DomainEventJsonSerializer` |
| `Miniclip.Core.ReadModels` | Read Abstractions | `IReadModelUnitOfWork`, projection handler base types |
| `Miniclip.Core.ReadModels.Projections` | Projection Infrastructure | `[HandlerPriority]` attribute, `IProjectionDispatcher`, ordered projection execution |
| `Miniclip.Core.EF` | EF Infrastructure | Generic EF Core base context |
| `Miniclip.Core.EventSourcing` | Event Sourcing Abstractions | `IEventStore<T>`, `IEventStoreSession`, `AggregateRepository<T>` |
| `Miniclip.Core.EventSourcing.EventStoreDB` | Event Sourcing Infrastructure | `EventStoreDbEventStore<T>`, `EventStoreSession`, `SystemTextJsonEventSerializer` |
| `Miniclip.Core.Messaging` | Messaging Abstractions | `IEventBus`, `IMessagePipeline`, `IMessageHandler<T>`, `IMessageMiddleware`, `IMessageSerializer`, `IMessageContext`, `IMessageEnvelope`, `IRetryPolicy`, `IDeadLetterHandler`, `ExponentialBackoffRetryPolicy`, `MessageHeaders`, `PipelineResult`, `MessageHandlerResult` |
| `Miniclip.Core.Messaging.Pipeline` | Messaging Pipeline | `MessagePipeline`, `TracingMiddleware`, `LoggingMiddleware`, `RetryMiddleware`, `IMessageHandlerRegistry`, `MessageHandlerRegistry`, `CompiledMessageHandler` |
| `Miniclip.Core.Messaging.Kafka` | Kafka Infrastructure | `KafkaConsumerHost`, `KafkaEventBus`, `KafkaDeadLetterHandler`, `TopicNaming`, `ConsumerGroupIdNaming`, `KafkaConsumerConfig`, `KafkaMessageMapper`, `KafkaConstants` |
| `Miniclip.Core.OpenTelemetry` | Observability | `OpenTelemetryActivity`, `OpenTelemetryMetrics`, OTel builder extension methods |
| `Miniclip.Core.ServiceDefaults` | Service Defaults | `SerilogConfiguration.AddStructuredLogging()` — Serilog with OTLP sink |
| `Miniclip.Simulator.Domain` | Domain | `Group`, `Team` aggregates, domain services, value objects |
| `Miniclip.Simulator.Application.Commands` | Application – Write | `GenerateGroupCommand`, `SimulateGroupCommand` handlers |
| `Miniclip.Simulator.Application.Queries` | Application – Read | `GroupStandingsQuery` handler |
| `Miniclip.Simulator.ReadModels` | Read Models | `GroupStandingsModel`, `MatchResultModel`, repository interfaces |
| `Miniclip.Simulator.ReadModels.Projections` | Projections | `ProjectionMessageHandler<TEvent>`, `GroupStandingsProjection`, `MatchResultProjection`, `RecalculatePositionService` |
| `Miniclip.Simulator.Infrastructure.Read` | Infrastructure – Read | `SimulatorReadDbContext`, repository implementations |
| `Miniclip.Simulator.Infrastructure.Write` | Infrastructure – Write | EF migrations only (empty model; legacy aggregate tables dropped) |
| `Miniclip.Simulator.Api` | API | `GroupsController`, configuration wiring, `TeamDataSeeder` |
| `Miniclip.Simulator.ReadModels.WebJob` | ReadModels Worker | Worker Service; hosts all `ProjectionsConsumerService<TAggregate>` instances; runs read DB migrations |
| `Miniclip.Simulator.AppHost` | Orchestration | .NET Aspire AppHost; provisions MySQL, KurrentDB, Kafka, API, WebJob |

---

## Key Domain Concepts

- **Group** — The core write-side aggregate. Stored as an event stream in KurrentDB (`group-{id}`). Owns a list of `TeamInfo` value object snapshots and `Match` entities. Capacity 2–6. Emits: `GroupCreated`, `TeamAdded`, `MatchScheduled`, `MatchPlayed`.
- **TeamInfo** — Value object `(Guid Id, string Name, int Strength)` captured at group creation. `Group` and `Match` use `TeamInfo` instead of `Team` references to respect aggregate boundaries.
- **Team** — An event-sourced aggregate stored in KurrentDB (`team-{id}`). Emits `TeamRegistered`. A fixed squad of 10 teams is seeded at startup by `TeamDataSeeder`. Strength (0–100) influences match outcomes.
- **Match** — An entity owned by `Group`. Has `TeamInfo HomeTeam`, `TeamInfo AwayTeam`, `Round`, and scores. Can only be simulated once (`IsPlayed`).
- **Fixture Scheduling** — Uses a **Round Robin** algorithm. Odd team counts add a dummy bye slot (internal to the scheduler, not a `Team` aggregate).
- **Match Simulation** — Uses a **Poisson distribution** based on each team's `Strength`. Home team gets a `1.1×` advantage multiplier.
- **MatchPlayed** — The domain event that drives all read-model updates. Published to Kafka after being committed to KurrentDB.
- **GroupStandings** — A read model tracking Points, Wins, Draws, Losses, GF/GA, GD, and Position per team. Rebuilt from `MatchPlayed` Kafka events.

---

## Domain Events

| Event | Aggregate | Stream | Published to Kafka |
|---|---|---|---|
| `TeamRegistered` | `Team` | `team-{id}` | No (seeder only) |
| `GroupCreated` | `Group` | `group-{id}` | No |
| `TeamAdded` | `Group` | `group-{id}` | No |
| `MatchScheduled` | `Group` | `group-{id}` | No |
| `MatchPlayed` | `Group` | `group-{id}` | **Yes** → `simulator.group` topic → WebJob projections |

---

## Patterns & Conventions

### Result Pattern
All operations return `Result` or `Result<T>` — **never throw exceptions for business rule violations**.

### CQRS
- **Commands** modify state and live in `Miniclip.Simulator.Application.Commands`. They use `IAggregateRepository<T>`.
- **Queries** read from denormalised read models in `Miniclip.Simulator.Application.Queries`.

### Domain Events
- Aggregates enqueue events via `Enqueue(IDomainEvent)` (from `AggregateRoot`) AND set their state directly in the constructor/factory (so the aggregate is immediately usable after `Create`).
- `Apply(IDomainEvent)` handles replay from KurrentDB only — it is not called during normal command processing.
- Events are committed to **KurrentDB** and published to **Kafka** by `EventStoreCommandBehavior` (single behavior handles both steps).
- `KafkaConsumerHost` (in the **WebJob**) is a `BackgroundService` that subscribes to a Kafka topic and forwards each message through the `IMessagePipeline` (middleware chain: `TracingMiddleware` → `LoggingMiddleware` → `RetryMiddleware`).
- `ProjectionMessageHandler<TEvent>` (registered as `IMessageHandler<TEvent>`) creates a **fresh DI scope per message**, checks idempotency, then dispatches to ordered `INotificationHandler<TDomainEvent>` projection handlers via `IProjectionDispatcher`.
- Idempotency: the `ProcessedEvents` table records each `event-id` + consumer group ID before committing.

### Kafka Topic & Consumer Group Naming
- **Topics** follow `simulator.{aggregate-kebab-case}`: `Group` → `simulator.group`.
- **Consumer groups** follow `simulator-projections-{aggregate}`: `Group` → `simulator-projections-group`.
- Both conventions are codified in `TopicNaming` and `ConsumerGroupIdNaming` helpers.

### Messaging Pipeline & Kafka Consumer Lifecycle
`KafkaConsumerHost` (`BackgroundService`) subscribes to one or more Kafka topics and processes each message through `IMessagePipeline`. The pipeline is a middleware chain registered outermost-first:
1. `TracingMiddleware` — starts an OTel span per message; tags `message-id`, `message-type`, `subscription-id`, and `correlation-id`; records errors on the span.
2. `LoggingMiddleware` — logs message receipt and outcome.
3. `RetryMiddleware` — retries transient failures using `IRetryPolicy` (`ExponentialBackoffRetryPolicy` by default); permanently fails messages that exhaust all attempts.

After the pipeline completes, permanently failed messages are forwarded to `IDeadLetterHandler` (`KafkaDeadLetterHandler`). Successfully processed messages are committed back to Kafka.

### Mediator
Uses the **Mediator** NuGet package (source-generated — **not MediatR**). Commands/queries implement `IRequest<TResponse>`; handlers implement `IRequestHandler<TRequest, TResponse>`. Notification handlers are discovered at compile time; no registration boilerplate needed.

### Versioning
API is versioned with `Asp.Versioning`. All routes follow `api/v{version}/[controller]`. Current version: `v1`.

### Error Mapping
`ResultExtensions.ToActionResult()` maps `Result` failures to HTTP status codes (400 / 404 / 204).

### EF Core
Only the **read side** uses EF Core (`SimulatorReadDbContext`). The write `DbContext` (`SimulatorWriteDbContext`) has an empty model — it exists only to carry the migration that dropped all legacy aggregate tables. Read DB migrations are run by the **WebJob** via `host.InitializeDatabases()` on startup.

### Observability
- **Structured logging** — both the API and WebJob call `builder.AddStructuredLogging()` (`Miniclip.Core.ServiceDefaults`), which configures Serilog with console JSON output and an optional OTLP log sink.
- **Traces** — `OpenTelemetryActivity.StartActivity(name)` wraps per-message Kafka processing; KurrentDB client, ASP.NET Core, MySQL, and Kafka producer/consumer instrumentation are all wired in.
- **Metrics** — `OpenTelemetryMetrics.RecordRetryAttempt()` / `RecordMessageFailed()` emit counters from the `Miniclip.Simulator.Kafka` meter. All telemetry is exported via OTLP.

---

## Testing

| Project | What it tests |
|---|---|
| `Miniclip.Simulator.Domain.UnitTests` | Aggregate logic, fixture scheduling, simulation |
| `Miniclip.Simulator.Application.Commands.UnitTests` | Command handler logic |
| `Miniclip.Simulator.Application.Queries.UnitTests` | Query handler logic |
| `Miniclip.Simulator.ReadModels.Projections.UnitTests` | `ProjectionMessageHandler` idempotency; projection handlers |
| `Miniclip.Simulator.ReadModels.Projections.IntegrationTests` | Full projection pipeline against a real read DB |
| `Miniclip.Simulator.ReadModels.WebJob.UnitTests` | WebJob infrastructure configuration (health checks, etc.) |
| `Miniclip.Core.Kafka.UnitTests` | _(empty — tests migrated to messaging projects)_ |
| `Miniclip.Simulator.Api.UnitTests` | Controller / result extension behaviour |
| `Miniclip.Simulator.Common.Tests` | Shared test helpers and builders |
| `Miniclip.Core.Tests` | Shared kernel tests |

---

## Further Reading

- [`docs/architecture.md`](docs/architecture.md) — Layer responsibilities, full request flow, dependency graph
- [`docs/domain-model.md`](docs/domain-model.md) — Aggregates, business rules, simulation algorithm, read model schema
- [`docs/observability.md`](docs/observability.md) — OpenTelemetry and Serilog setup
- [`docs/adr/`](docs/adr/) — Architecture Decision Records
- [`docs/event-sourcing/PLAN.md`](docs/event-sourcing/PLAN.md) — Event Sourcing migration phases (all complete)

---

## Running Locally

```bash
cd src/Miniclip.Simulator.AppHost
dotnet user-secrets set "Parameters:mysql-password" "<your-password>"
dotnet run
```

Aspire provisions MySQL, KurrentDB, Kafka, the ReadModels WebJob, and the API. Read DB migrations run automatically in the WebJob before the API starts. Team seeding runs in the API on startup.
