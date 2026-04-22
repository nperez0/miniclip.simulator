# Copilot Instructions

## Agent Guidelines
- **Correct the user's English when responding to their requests.**

---

# Miniclip Simulator — Project Context

> This is a mirror of [`AI.md`](../AI.md), which is the canonical source of truth.
> Update `AI.md` first; then sync this file.

---

## Project Overview

**Miniclip Simulator** is a football group-stage simulator REST API.
It allows clients to generate a group with random teams, simulate all matches in the group, and query the final standings with results.

The solution lives under `src/` and is orchestrated by **.NET Aspire** using **KurrentDB** (formerly EventStoreDB) as the write store, **MySQL** as the read store, and **Kafka** as the distributed event bus.
The stack targets **.NET 10**.

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
2. `EventStoreCommandBehavior` — after the handler succeeds: calls `IEventStoreSession.CommitAsync()` then iterates `session.GetCommittedEvents()` and calls `ICommittedEventPublisher.PublishAsync()` for each. `CommittedEventPublisher` maps each committed domain event to an `IIntegrationEvent` via `IIntegrationEventMapperRegistry`, then publishes it to `IEventBus` (Kafka). Events without a registered mapper are silently skipped.

> `ReadModelUnitOfWorkBehavior` was removed in Phase 4 — the read side is now updated exclusively by the ReadModels WebJob.

---

## Project Structure

| Project | Layer | Responsibility |
|---|---|---|
| `Miniclip.Core` | Shared Kernel | `Result<T>`, `ExceptionBase`, string/enumerable extensions |
| `Miniclip.Core.Domain` | Domain Abstractions | `AggregateRoot`, `IAggregateRepository<T>`, `IDomainEvent` |
| `Miniclip.Core.Application` | Application Abstractions | `ICommand`, `IQuery`, pipeline behaviour base types (`LoggingBehavior`, `EventStoreCommandBehavior`), `ICommittedEventPublisher`, `CommittedEventPublisher`, `IIntegrationEventMapper<T>`, `IIntegrationEventMapperRegistry` |
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
| `Miniclip.Simulator.Application.Queries` | Application – Read | `GroupStandingsQuery` handler — returns team standings + match results in a single `GroupStandingsDto` |
| `Miniclip.Simulator.ReadModels` | Read Models | `GroupStandingsModel`, `MatchResultModel`, repository interfaces |
| `Miniclip.Simulator.ReadModels.Projections` | Projections | `ProjectionMessageHandler<TEvent>`, `GroupStandingsProjection`, `MatchResultProjection`, `RecalculatePositionService` |
| `Miniclip.Simulator.IntegrationEvents` | Integration Events | `MatchPlayedIntegrationEvent`, `MatchPlayedIntegrationEventMapper` — versioned integration event contracts published to Kafka |
| `Miniclip.Simulator.Infrastructure.Read` | Infrastructure – Read | `SimulatorReadDbContext`, repository implementations |
| `Miniclip.Simulator.Infrastructure.Write` | Infrastructure – Write | EF migrations only (empty model; legacy aggregate tables dropped) |
| `Miniclip.Simulator.Api` | API | `GroupsController`, `CorrelationIdMiddleware`, configuration wiring, `TeamDataSeeder` |
| `Miniclip.Simulator.ReadModels.WebJob` | ReadModels Worker | Worker Service; hosts all `ProjectionsConsumerService<TAggregate>` instances; runs read DB migrations |
| `Miniclip.Simulator.AppHost` | Orchestration | .NET Aspire AppHost; provisions MySQL, KurrentDB, Kafka, API, WebJob |

---

## Key Domain Concepts

- **Group** — The core write-side aggregate. Stored as an event stream in KurrentDB (`group-{id}`). Owns a list of `TeamInfo` value object snapshots and `Match` entities. Capacity 2–6. Emits: `GroupCreated`, `TeamAdded`, `MatchScheduled`, `MatchPlayed`.
- **TeamInfo** — Value object `(Guid Id, string Name, int Strength)` captured at group creation.
- **Team** — An event-sourced aggregate stored in KurrentDB (`team-{id}`). Emits `TeamRegistered`. A fixed squad of 10 teams is seeded at startup by `TeamDataSeeder`. Strength (0–100) influences match outcomes.
- **Match** — An entity owned by `Group`. Has `TeamInfo HomeTeam`, `TeamInfo AwayTeam`, `Round`, and scores. Can only be simulated once (`IsPlayed`).
- **Fixture Scheduling** — Uses a **Round Robin** algorithm. Odd team counts add a dummy bye slot.
- **Match Simulation** — Uses a **Poisson distribution** based on each team's `Strength`. Home team gets a `1.1×` advantage multiplier.
- **MatchPlayed** — The domain event that drives all read-model updates. Published to Kafka after being committed to KurrentDB.

---

## Domain Events

| Event | Aggregate | Stream | Published to Kafka |
|---|---|---|---|
| `TeamRegistered` | `Team` | `team-{id}` | No (seeder only) |
| `GroupCreated` | `Group` | `group-{id}` | No |
| `TeamAdded` | `Group` | `group-{id}` | No |
| `MatchScheduled` | `Group` | `group-{id}` | No |
| `MatchPlayed` | `Group` | `group-{id}` | **Yes** → mapped to `MatchPlayedIntegrationEvent` → `simulator.group` topic → WebJob projections |

---

## Patterns & Conventions

### Result Pattern
All operations return `Result` or `Result<T>` — **never throw exceptions for business rule violations**.

### CQRS
- **Commands** modify state and live in `Miniclip.Simulator.Application.Commands`. They use `IAggregateRepository<T>`.
- **Queries** read from denormalised read models in `Miniclip.Simulator.Application.Queries`. The `GroupStandingsQueryHandler` handles `GroupStandingsQuery` and returns both team standings and match results in a single `GroupStandingsDto`.

### Domain Events
- Aggregates enqueue events via `Enqueue(IDomainEvent)` AND set their state directly in the constructor/factory.
- `Apply(IDomainEvent)` handles replay from KurrentDB only — not called during normal command processing.
- Events are committed to **KurrentDB** and published to **Kafka** by `EventStoreCommandBehavior` (single behavior handles both steps). Domain events are not published directly to Kafka; instead, `CommittedEventPublisher` maps them to **integration events** via `IIntegrationEventMapperRegistry`. Only domain events that have a registered `IIntegrationEventMapper<T>` are forwarded to Kafka. This decouples the internal domain model from the external contract (`Miniclip.Simulator.IntegrationEvents`).
- `KafkaConsumerHost` (in the **WebJob**) is a `BackgroundService` that subscribes to a Kafka topic and forwards each message through the `IMessagePipeline` (middleware chain: `TracingMiddleware` → `LoggingMiddleware` → `RetryMiddleware`).
- `ProjectionMessageHandler<TEvent>` (registered as `IMessageHandler<TEvent>`) creates a **fresh DI scope per message**, checks idempotency, then dispatches to ordered projection handlers via `IProjectionDispatcher`.
- Idempotency: the `ProcessedEvents` table records each `event-id` + consumer group ID before committing.

### Kafka Topic & Consumer Group Naming
- **Topics:** `simulator.{aggregate-kebab-case}` — e.g. `Group` → `simulator.group`
- **Consumer groups:** `simulator-projections-{aggregate}` — e.g. `simulator-projections-group`

### Messaging Pipeline & Kafka Consumer Lifecycle
`KafkaConsumerHost` (`BackgroundService`) subscribes to one or more Kafka topics and processes each message through `IMessagePipeline`. The pipeline is a middleware chain registered outermost-first:
1. `TracingMiddleware` — starts an OTel span per message; tags `message-id`, `message-type`, `subscription-id`, and `correlation-id`; records errors on the span.
2. `LoggingMiddleware` — logs message receipt and outcome.
3. `RetryMiddleware` — retries transient failures using `IRetryPolicy` (`ExponentialBackoffRetryPolicy` by default); permanently fails messages that exhaust all attempts.

After the pipeline completes, permanently failed messages are forwarded to `IDeadLetterHandler` (`KafkaDeadLetterHandler`). Successfully processed messages are committed back to Kafka.

### Mediator
Uses the **Mediator** NuGet package (source-generated — **not MediatR**). Commands/queries implement `IRequest<TResponse>`; handlers implement `IRequestHandler<TRequest, TResponse>`.

### Versioning
API is versioned with `Asp.Versioning`. All routes follow `api/v{version}/[controller]`. Current version: `v1`.

### Correlation ID Propagation
`CorrelationIdMiddleware` runs early in the ASP.NET Core pipeline. It reads the `X-Correlation-Id` header (generating a new `Guid` if absent) and stores both `CorrelationId` and `CausationId` on `IMutablePropagationContext`. The correlation ID is written back to the response header. This ensures every request and its downstream Kafka messages share a traceable ID.

### Error Mapping
`ResultExtensions.ToActionResult()` maps `Result` failures to HTTP status codes (400 / 404 / 204).

### EF Core
Only the **read side** uses EF Core (`SimulatorReadDbContext`). Read DB migrations are run by the **WebJob** on startup.

### Observability
- **Structured logging** — `builder.AddStructuredLogging()` (Serilog with console JSON + OTLP sink).
- **Traces** — `OpenTelemetryActivity.StartActivity(name)` per Kafka message; KurrentDB, ASP.NET Core, MySQL, and Kafka instrumentation wired in.
- **Metrics** — `kafka.retry.attempts` / `kafka.messages.failed` counters from `Miniclip.Simulator.Kafka` meter, exported via OTLP.


### Configuration

- **Strongly-typed config objects** -- services receive a `*Config` record instead of `IConfiguration`. `IConfiguration` is consumed only inside `*Configuration` registration classes.
- **Naming** -- config data objects are named `<Feature>Config` (e.g., `HealthCheckConfig`). Never `<Feature>Options` or `<Feature>Settings`.
- **Registration** -- the `*Config` record is populated from `IConfiguration` inside the `AddXxxDependencies` extension method and registered as a singleton: `services.AddSingleton(new HealthCheckConfig { Port = configuration[...] })`.

### Code Style

- **No alignment spaces** — do not pad assignment operators or dictionary indexers with extra spaces to visually align values across lines.
- **Arrays over lists** — prefer arrays (`T[]`) or `IReadOnlyList<T>` by default. Only use `List<T>` where there is a clear benefit (e.g., the collection is built incrementally via `Add` or requires frequent mutations).
- **`CancellationToken` naming** — always name the parameter `cancellationToken`. Use a different name only when it meaningfully aids clarity at the call site.
- **No XML doc comments** — do not add `///` documentation comments to methods or classes. The only exception is API controller endpoints.
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

### Unit Test Conventions

All unit tests follow the **abstract `When*` / concrete `With*`** pattern backed by `TestBase<TSut>` or `AsyncTestBase<TSut>` (from `Miniclip.Core.Tests`).

#### Structure
- **Abstract `When<Context>` class** -- extends `TestBase<TSut>` (sync) or `AsyncTestBase<TSut>` (async).
  - No `[TestFixture]` attribute.
  - `Given()` / `GivenAsync()` -- sets up shared state (mocks, configuration, fixtures).
  - `CreateSystemUnderTest()` -- constructs and returns the SUT; called by the base after `Given()`.
  - `When()` / `WhenAsync()` -- executes the action under test (override when needed).
- **Concrete `With<Scenario>` classes** -- one per test scenario; inherit the abstract base.
  - No `[TestFixture]` attribute.
  - Override `Given()` only when the scenario requires different setup from the base.
  - Each `[Test]` method contains a **single assertion**.
  - Name tests as `Should<ExpectedBehaviour>`.

#### Base class lifecycle (`[OneTimeSetUp]`)
`Given() -> CreateSystemUnderTest() -> When()`

#### DI registration tests
When testing `IServiceCollection` configuration, use `TestBase<ServiceProvider>`:
- `Given()` sets up `IConfiguration`.
- `CreateSystemUnderTest()` builds and returns the `ServiceProvider`.
- Concrete classes assert that specific services are / are not registered via `Sut!.GetService<T>()` / `GetServices<T>()`.
- Do **not** name the config property `Configuration` inside a namespace ending in `.Configuration` -- it causes `CS0118`; use `Config` instead.

#### Key rules
- `Fixture` (AutoFixture + AutoNSubstitute) is provided by the base -- use `Fixture.Freeze<T>()` for mocks.
- `Sut` is the typed SUT instance, available after `CreateSystemUnderTest()` runs.
- Every test project has `GlobalUsings.cs` with `global using NUnit.Framework; Shouldly; NSubstitute; Microsoft.Extensions.DependencyInjection`.
- No `[TestFixture]` on concrete classes (NUnit discovers them via the abstract base).

---

## Further Reading

- [`docs/architecture.md`](../docs/architecture.md) — Layer responsibilities, full request flow, dependency graph
- [`docs/domain-model.md`](../docs/domain-model.md) — Aggregates, business rules, simulation algorithm, read model schema
- [`docs/observability.md`](../docs/observability.md) — OpenTelemetry and Serilog setup
- [`docs/adr/001-result-pattern.md`](../docs/adr/001-result-pattern.md) — Result pattern ADR
- [`docs/adr/002-cqrs-separate-dbcontexts.md`](../docs/adr/002-cqrs-separate-dbcontexts.md) — CQRS separate DbContexts ADR
- [`docs/adr/003-domain-events-post-commit.md`](../docs/adr/003-domain-events-post-commit.md) — superseded; documents the original in-process dispatch approach and how it evolved to `EventStoreCommandBehavior` + Kafka
- [`docs/adr/004-source-generated-mediator.md`](../docs/adr/004-source-generated-mediator.md) — source-generated Mediator ADR
- [`docs/event-sourcing/PLAN.md`](../docs/event-sourcing/PLAN.md) — Event Sourcing migration phases (all complete)

---

## Running Locally

```bash
cd src/Miniclip.Simulator.AppHost
dotnet user-secrets set "Parameters:mysql-password" "<your-password>"
dotnet run
```

Aspire provisions MySQL, KurrentDB, Kafka, the ReadModels WebJob, and the API. Read DB migrations run automatically in the WebJob before the API starts.

---

## Sequential Thinking

Use the `sequentialthinking` tool for tasks that involve non-obvious tradeoffs or multi-step reasoning. Skip it for straightforward single-file changes.

**Use it for:**
- Designing a feature that spans multiple projects (entity → command → handler → projection → API)
- Saga design, compensation/rollback logic
- Architectural decisions or domain modelling (new aggregate, value object design)
- Planning changes that touch multiple DbContexts or migrations
- Diagnosing architectural violations or subtle bugs
- Designing a new swap rule engine flow

**Skip it for:**
- Simple additions (enum value, DTO property, mapping column, feature toggle)
- Renames, small bug fixes, test additions following an existing pattern
- Tasks where the correct pattern is already obvious from the AGENTS.md files