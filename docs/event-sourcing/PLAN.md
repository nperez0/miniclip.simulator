# Event Sourcing Migration Plan

## Goal

Replace state-based persistence and in-process event dispatching with:
- **EventStoreDB** as the append-only event store (source of truth for the write side)
- **Kafka** as the distributed event bus (replacing in-process Mediator publishing)

---

## Technology Decisions

| Concern | Technology | Rationale |
|---|---|---|
| Event Store | EventStoreDB | Per-aggregate streams, optimistic concurrency, native replay |
| Event Bus | Kafka | Ordered delivery per partition (key = `AggregateId`), consumer groups |
| Read Model Store | MySQL (unchanged) | Materialised views; no reason to change |

- Topic naming: `simulator.{event-name}` (e.g. `simulator.match-played`)
- Stream naming: `{type}-{id}` (e.g. `group-{guid}`, `team-{guid}`)
- Category streams (`$ce-{type}`) used by `GetAllAsync` via the built-in `$by_category` projection

---

## Phase Overview

| # | Name | Status |
|---|---|---|
| 0 | Planning & Documentation | ✅ Done |
| 1 | EventStoreDB — Core Abstractions | ✅ Done |
| 2 | EventStoreDB — Write Side Migration | ✅ Done |
| 3 | Kafka — Event Bus | ✅ Done |
| 4 | Kafka — Read Side Consumers | ✅ Done |
| 5 | Testing & Hardening | ✅ Done |

---

## Phase 1 — EventStoreDB: Core Abstractions

**Goal:** Introduce EventStoreDB to the AppHost and define all event-sourcing abstractions without breaking existing behaviour.

- [x] EventStoreDB container visible and healthy in the Aspire dashboard
- [x] `AggregateRoot` extended with `Version`, `Apply`, `Enqueue`, `ReplayEvent`
- [x] `IEventStore<T>`, `IEventSerializer`, `IEventStoreSession` defined
- [x] `EventStoreDbEventStore<T>` and `EventStoreSession` implemented
- [x] All existing tests pass

---

## Phase 2 — EventStoreDB: Write Side Migration

**Goal:** The write side persists domain events to EventStoreDB instead of aggregate state in MySQL.

Key decisions:
- `IUnitOfWork` removed from the write path; ESDB optimistic concurrency replaces DB transactions
- `EventStoreCommandBehavior` commits `IEventStoreSession` after the handler returns
- `SimulatorWriteDbContext` Group/Match/Team aggregate configurations removed

- [x] `GenerateGroupCommand` and `SimulateGroupCommand` persist via EventStoreDB
- [x] EF Core write DbContext aggregate configuration removed
- [x] All command handler tests pass

---

## Phase 3 — Kafka: Event Bus

**Goal:** Replace in-process Mediator publishing with Kafka.

Key decisions:
- Events published to Kafka **after** a successful ESDB append via `DomainEventPublisherBehavior`
- `IEventBus` defined in `Miniclip.Core.Application`; `KafkaEventBus` is the implementation
- Kafka message key = `AggregateId.ToString()`

- [x] Kafka and Kafka UI containers visible in Aspire dashboard
- [x] `MatchPlayed` events visible in `simulator.match-played` topic after simulation
- [x] `DomainEventPublisherBehavior` publishes to Kafka via `IEventBus`
- [x] All existing API behaviour unchanged

---

## Phase 4 — Kafka: Read Side Consumers

**Goal:** Projections become Kafka consumer hosted services; `ReadModelUnitOfWorkBehavior` removed.

Key decisions:
- `ProjectionsConsumerService<TEvent>` is a `BackgroundService` consuming a single topic
- Idempotency: `ProcessedEvents` table records `event-id` + consumer group ID before commit
- `IServiceScopeFactory` for per-message DI scope — avoids captive dependency with `DbContext`
- Consumer group ID: `simulator-projections-{event-name}`

- [x] `GroupStandingsProjection` and `MatchResultProjection` driven by Kafka consumers
- [x] `ReadModelUnitOfWorkBehavior` removed from the Mediator pipeline
- [x] Read models correctly populated after simulation
- [x] Consumer group offset visible in Kafka UI
- [x] All query tests pass

---

## Phase 5 — Testing & Hardening

**Goal:** Production-readiness: resilience, integration tests, configuration cleanup, Team migration.

Key deliverables:
- **Retry policy:** `ExponentialBackoffRetryPolicy` (configurable max attempts and base delay)
- **Dead-letter routing:** Permanently failed messages passed to `OnDeadLetterAsync` and committed
- **Idempotency correctness:** Duplicate-check before deserialization
- **`KafkaConsumerService` refactor:** Consumer built internally via abstract `BuildConsumer(ConsumerConfig)`; `IServiceScopeFactory` injected for per-message scoping; factory lambda in `AddHostedService` eliminated
- **Configuration split:** `DatabaseConfiguration` decomposed into `ReadModelsConfiguration`, `EventStoreDbConfiguration`, `KafkaConfiguration`
- **Integration tests:** Full projection pipeline tested end-to-end against an in-memory read DB
- **Team migration:** `Team` aggregate moved from MySQL to EventStoreDB; `TeamDataSeeder` seeds 10 teams on startup; all legacy write-side tables (Groups, GroupTeams, Matches, Teams) dropped via migration

- [x] Integration tests cover the full projection pipeline
- [x] Dead-letter routing on permanent failure
- [x] Exponential backoff retry policy
- [x] `KafkaConsumerService` owns its own `IConsumer` lifecycle
- [x] Scoped services resolved per message via `IServiceScopeFactory`
- [x] `Team` stored in EventStoreDB; MySQL write-side tables fully removed