# Event Sourcing Migration Plan

## Goal

Replace the current state-based persistence + in-process event dispatching with:
- **EventStoreDB** as the append-only event store (source of truth for the write side)
- **Kafka** as the distributed event bus (replacing in-process Mediator publishing)

This enables full event history, read model rebuilding from scratch, and ordered event delivery per aggregate.

---

## Technology Decisions

| Concern | Technology | Rationale |
|---|---|---|
| Event Store | EventStoreDB | Purpose-built: per-aggregate streams, optimistic concurrency, native replay |
| Event Bus | Kafka | Ordered delivery per partition (partition key = `GroupId`), consumer groups |
| Read Model Store | MySQL (unchanged) | Read models are still materialized views, no reason to change |
| Write Model Store | EventStoreDB (replaces EF Core for aggregates) | State is derived from events |

### Kafka Topic Strategy
- Topic: `simulator.{event-name}` (e.g. `simulator.match-played`)
- Partition key: `GroupId` — guarantees ordering of all events for the same group

### Stream Naming (EventStoreDB)
- Pattern: `{aggregate-type}-{aggregate-id}` (e.g. `group-550e8400-e29b-41d4-a716-446655440000`)

---

## Phase Overview

| # | Name | Status | Branch | Spec |
|---|---|---|---|---|
| 0 | Planning & Documentation | ✅ Done | `main` | *(this file)* |
| 1 | EventStoreDB — Core Abstractions | ✅ Done | `feat/phase-1-esdb-abstractions` | [`PHASE-1.md`](PHASE-1.md) |
| 2 | EventStoreDB — Write Side Migration | ✅ Done | `feat/phase-2-esdb-write` | [`PHASE-2.md`](PHASE-2.md) |
| 3 | Kafka — Event Bus | ⬜ Pending | `feat/phase-3-kafka-bus` | [`PHASE-3.md`](PHASE-3.md) |
| 4 | Kafka — Read Side Consumers | ⬜ Pending | `feat/phase-4-kafka-read` | `PHASE-4.md` *(created before Phase 4 starts)* |
| 5 | Testing & Hardening | ⬜ Pending | `feat/phase-5-hardening` | `PHASE-5.md` *(created before Phase 5 starts)* |

---

## Phase 1 — EventStoreDB: Core Abstractions

**Goal:** Introduce EventStoreDB to the AppHost and define all event-sourcing abstractions. No existing behaviour is broken.

### Projects Affected
| Project | Change |
|---|---|
| `Miniclip.Simulator.AppHost` | Add EventStoreDB resource |
| `Miniclip.Core.Domain` | Evolve `AggregateRoot` |
| `Miniclip.Core.EventSourcing` *(new)* | Abstractions: `IEventStore`, `IEventSerializer`, `EventEnvelope` |
| `Miniclip.Core.EventSourcing.EventStoreDB` *(new)* | ESDB client implementation |

### Key Design Decisions
- `AggregateRoot` gains `Version` (expected stream version for optimistic concurrency) and an `Apply(IDomainEvent)` dispatch method.
- Events are **not** double-dispatched through `Enqueue` + `Apply` on load — `Apply` is only called during stream replay.
- `EventEnvelope` wraps a domain event with metadata: `EventId`, `EventType`, `AggregateId`, `Version`, `OccurredOn`.
- `IEventStore` is defined in `Miniclip.Core.Domain` (or `Miniclip.Core.EventSourcing`) — NOT in infrastructure.

### Definition of Done
- [x] EventStoreDB container visible and healthy in the Aspire dashboard
- [x] `AggregateRoot` has `Version`, `Apply`, and `DequeueUncommittedEvents` updated
- [x] `IEventStore<T>`, `IEventSerializer`, `EventEnvelope<T>` defined
- [x] EventStoreDB client project compiles and connects
- [x] All existing tests pass (write side not yet migrated)

---

## Phase 2 — EventStoreDB: Write Side Migration

**Goal:** The write side persists domain events to EventStoreDB instead of aggregate state in MySQL.

### Projects Affected
| Project | Change |
|---|---|
| `Miniclip.Core.Application` | Replace `CommandUnitOfWorkBehavior` with `EventSourcedCommandBehavior` |
| `Miniclip.Core.EventSourcing.EventStoreDB` | Implement `EventStoreRepository<T>` |
| `Miniclip.Simulator.Infrastructure.Write` | Replace `GroupsRepository`; remove EF Core aggregate tables |
| `Miniclip.Simulator.Application.Commands.UnitTests` | Update mocks for new repository contract |

### Key Design Decisions
- `IUnitOfWork` is **removed** from the write path. ESDB's optimistic concurrency (stream version check on append) replaces DB transactions.
- `EventSourcedCommandBehavior` replaces `CommandUnitOfWorkBehavior`: it catches `WrongExpectedVersionException` and maps it to a conflict result.
- `GroupsRepository` loads a group by replaying its ESDB stream; saves by appending new events.
- `SimulatorWriteDbContext` is removed (aggregate tables only — the read `DbContext` and MySQL remain untouched).

### Definition of Done
- [x] `GenerateGroupCommand` persists via EventStoreDB
- [x] `SimulateGroupCommand` persists via EventStoreDB
- [x] EF Core write `DbContext` aggregate configuration and `SimulatorUnitOfWork` removed
- [x] All existing command handler tests updated and passing

---

## Phase 3 — Kafka: Event Bus

**Goal:** Replace in-process Mediator publishing (`DomainEventPublisherBehavior`) with Kafka.

### Projects Affected
| Project | Change |
|---|---|
| `Miniclip.Simulator.AppHost` | Add Kafka + Kafka UI resources |
| `Miniclip.Core.Application` | Replace `DomainEventPublisherBehavior` with Kafka publisher behavior |
| `Miniclip.Core.Kafka` *(new)* | `IEventBus`, Kafka producer implementation |
| `Miniclip.Simulator.Infrastructure.Write` | Wire Kafka producer |

### Key Design Decisions
- Events are published to Kafka **after** a successful ESDB append (not before).
- `IEventBus` abstraction defined in `Miniclip.Core.Domain` or `Miniclip.Core.Application`.
- Kafka message key = `GroupId.ToString()` — guarantees partition ordering per group.
- Topic auto-creation disabled; topics pre-declared in AppHost.
- Projections still work in-process via Mediator at the end of this phase (they are migrated in Phase 4).

### Definition of Done
- [ ] Kafka and Kafka UI containers visible and healthy in Aspire dashboard
- [ ] `MatchPlayed` events visible in `simulator.match-played` topic after simulation
- [ ] `DomainEventPublisherBehavior` publishes to Kafka via `IEventBus` (`IPublisher` direct call removed)
- [ ] Projections continue to work via `MatchPlayedKafkaRelayService` (Kafka→Mediator bridge)
- [ ] All existing API behaviour unchanged (end-to-end)

---

## Phase 4 — Kafka: Read Side Consumers

**Goal:** Projections become proper Kafka consumer hosted services, replacing in-process `INotificationHandler<T>` handlers.

### Projects Affected
| Project | Change |
|---|---|
| `Miniclip.Simulator.ReadModels.Projections` | Refactor handlers to be Kafka-consumer-driven |
| `Miniclip.Core.ReadModels.Projections` | Update `[HandlerPriority]` to work with ordered consumer dispatch |
| `Miniclip.Core.Kafka` | Add consumer infrastructure (hosted service base, offset tracking) |
| `Miniclip.Simulator.Infrastructure.Read` | Add Kafka consumer registration |

### Key Design Decisions
- Consumers use a **dedicated consumer group** (`simulator-projections`).
- `[HandlerPriority]` ordering is preserved: within a consumed message, ordered handlers are invoked in sequence.
- Idempotency: projections check if the event has already been applied (via `EventId` stored on the read model).
- At-least-once delivery is safe due to idempotency checks.

### Definition of Done
- [ ] `GroupStandingsProjection` and `MatchResultProjection` run as Kafka consumers
- [ ] `INotificationHandler<MatchPlayed>` implementations removed
- [ ] Read models correctly populated after simulation
- [ ] Consumer group offset visible in Kafka UI
- [ ] All existing query tests pass

---

## Phase 5 — Testing & Hardening

**Goal:** Full integration test coverage and production-readiness concerns.

### Topics
- **Integration tests**: Testcontainers for EventStoreDB + Kafka (replace in-memory fakes)
- **Snapshotting**: For aggregates with many events (e.g. a group with 15 matches), snapshot every N events
- **Dead-Letter Topic**: Failed projection messages routed to `simulator.dlq`
- **Observability**: OpenTelemetry spans for ESDB appends and Kafka produce/consume

### Definition of Done
- [ ] Integration test project covers: generate group → simulate → read standings (full flow)
- [ ] Snapshot written after N events; aggregate loads from snapshot + tail
- [ ] DLQ topic receives messages on projection failure
- [ ] ESDB and Kafka operations appear in Aspire telemetry dashboard
