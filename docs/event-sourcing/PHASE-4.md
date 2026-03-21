# Phase 4 — Kafka: Read Side Consumers

> **Status:** ✅ Complete
> **Branch:** `feat/phase-4-kafka-read`
> **Depends on:** Phase 3 complete ✅
> **Must not break:** all existing API behaviour and tests must remain green during the transition

---

## Goal

Replace the temporary `MatchPlayedKafkaRelayService` (Kafka→Mediator bridge) with a proper
generic `ProjectionsConsumerService<TEvent>` that directly manages the `IReadModelUnitOfWork`
transaction and adds idempotency tracking via a `ProcessedEvents` MySQL table.

---

## Current State (after Phase 3)

```
Kafka consumer (BackgroundService)
  └─ MatchPlayedKafkaRelayService       ← temporary bridge
       └─ publisher.Publish(MatchPlayed) via Mediator
            └─ GroupStandingsProjection  ← adds to EF change tracker, no SaveChanges
            └─ MatchResultProjection     ← adds to EF change tracker, no SaveChanges
            ✗  SaveChanges never called  ← BUG: relay service omits the UoW step
```

Two problems exist in the relay:

1. **Missing `SaveChanges`** — the relay calls `publisher.Publish` but never wraps it in
   `IReadModelUnitOfWork.BeginTransaction / SaveChanges / Commit`, so projection writes
   are never flushed to MySQL.
2. **No idempotency** — Kafka guarantees at-least-once delivery. A re-delivered message
   would duplicate read model rows without a processed-events guard.

After Phase 4:

```
Kafka consumer (BackgroundService)
  └─ ProjectionsConsumerService<MatchPlayed>
       ├─ Idempotency check (ProcessedEvents table) — skip if already processed
       ├─ uow.BeginTransactionAsync()
       ├─ publisher.Publish(MatchPlayed) via Mediator
       │    └─ GroupStandingsProjection
       │    └─ MatchResultProjection
       ├─ processedEventsRepository.Add(eventId, consumerGroup)
       ├─ uow.SaveChangesAsync()
       └─ uow.CommitAsync()          ← one atomic MySQL commit
```

---

## Design Decisions

### 1. Keep `IPublisher.Publish` inside the consumer

The projection handlers implement `INotificationHandler<MatchPlayed>` and are ordered by
`[HandlerPriority]`. Dispatching via `IPublisher` preserves that ordering without the
consumer needing to know about individual handlers. Adding a new projection handler in the
future requires only registering it — the consumer is unchanged.

### 2. Idempotency via `ProcessedEvents` table

A dedicated table `ProcessedEvents` with composite PK `(EventId, ConsumerGroup)` tracks which
Kafka messages have been handled. The insert is in the **same database transaction** as the
projection writes, making the check-then-write atomic.

| Column | Type | Notes |
|---|---|---|
| `EventId` | `CHAR(36)` | Value of the `event-id` Kafka header |
| `ConsumerGroup` | `VARCHAR(100)` | Consumer group that processed the event |
| `ProcessedAt` | `DATETIME(6)` | UTC timestamp of processing |

Flow:
1. Read `event-id` from Kafka header.
2. Query `ProcessedEvents` — if found, **return** (skip). Kafka offset still committed.
3. Begin MySQL transaction.
4. Dispatch via Mediator; append `ProcessedEvents` row to EF change tracker.
5. `SaveChanges` (writes projections + `ProcessedEvents` atomically).
6. Commit transaction.

### 3. One consumer class, one consumer group

All projection handlers for `MatchPlayed` are dispatched by a single
`ProjectionsConsumerService<MatchPlayed>` under consumer group `simulator-projections`. This
preserves partition ordering (all events for the same `GroupId` are processed sequentially)
without requiring separate consumer groups per handler.

### 4. Transactional scope per Kafka message

The consumer creates a new `IServiceScope` for each message. Within that scope, all
services share the same `SimulatorReadDbContext` instance:

- `IReadModelUnitOfWork` wraps the context's transaction.
- `IPublisher` resolves the projection handlers from the same scope.
- Handlers receive `IGroupStandingsRepository` / `IMatchResultsRepository` that track
  against the same context — ensuring the transaction covers all writes.

### 5. `ProcessedEventModel` lives in `Miniclip.Simulator.ReadModels`

`IProcessedEventsRepository` follows the same layer convention as
`IMatchResultsRepository` and `IGroupStandingsRepository`:
the interface and model in `Miniclip.Simulator.ReadModels`, the EF implementation and
configuration in `Miniclip.Simulator.Infrastructure.Read`.

### 6. Generic `ProjectionsConsumerService<TEvent>` — one class, any event type

`TEvent` has a single responsibility: deriving the Kafka topic at construction time via
`TopicNaming.ForType<TEvent>()`. Everything else — deserialization, Mediator dispatch,
UoW management — works against `IDomainEvent` and is therefore event-type-agnostic.

Because `IPublisher.Publish(IDomainEvent)` dispatches by the **actual runtime type**
returned from `IEventSerializer.Deserialize`, the correct `INotificationHandler<T>`
implementations are invoked for every event type. Registering a consumer for a new event
type in Phase 5+ is a single DI call:

```csharp
services.AddHostedService<ProjectionsConsumerService<GroupCreated>>();
```

No additional code is needed — only the projection handler and the DI registration.

`ProjectionsConsumerService<TEvent>` lives in `Miniclip.Simulator.ReadModels.Projections`
because it depends on simulator-specific `IProcessedEventsRepository`. `IReadModelUnitOfWork`
is already reachable transitively through:
`ReadModels.Projections` → `Core.Kafka` → `Core.Application` → `Core.ReadModels`

---

## Projects Affected

| Project | Change |
|---|---|
| `Miniclip.Core.Kafka` | Add `TopicNaming.ForType<TEvent>()` |
| `Miniclip.Simulator.ReadModels` | Add `ProcessedEventModel`; add `IProcessedEventsRepository` |
| `Miniclip.Simulator.Infrastructure.Read` | Add `ProcessedEventsConfiguration`; add `ProcessedEventsRepository`; add EF migration |
| `Miniclip.Simulator.ReadModels.Projections` | Remove `MatchPlayedKafkaRelayService`; add `ProjectionsConsumerService<TEvent>` |
| `Miniclip.Simulator.Api` | Register `IProcessedEventsRepository`; swap `AddHostedService` registration |

---

## Implementation Steps

### Step 1 — Add `TopicNaming.ForType<TEvent>()` to `Miniclip.Core.Kafka`

Add a type-parameter overload alongside the existing instance overload:

```csharp
// TopicNaming.cs
public static string ForType<TEvent>() where TEvent : IDomainEvent
    => $"simulator.{PascalCasePattern.Replace(typeof(TEvent).Name, "-$1").ToLowerInvariant()}";
```

The two overloads are consistent — `TopicNaming.For(new MatchPlayed(...))` and
`TopicNaming.ForType<MatchPlayed>()` return the same string.

### Step 2 — Add `ProcessedEventModel` to `Miniclip.Simulator.ReadModels`

```csharp
// Models/ProcessedEventModel.cs
namespace Miniclip.Simulator.ReadModels.Models;

public class ProcessedEventModel
{
    public required string EventId { get; init; }
    public required string ConsumerGroup { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}
```

### Step 3 — Add `IProcessedEventsRepository` to `Miniclip.Simulator.ReadModels`

```csharp
// Repositories/Write/IProcessedEventsRepository.cs
namespace Miniclip.Simulator.ReadModels.Repositories.Write;

public interface IProcessedEventsRepository
{
    Task<bool> ContainsAsync(string eventId, string consumerGroup, CancellationToken cancellationToken);
    void Add(string eventId, string consumerGroup);
}
```

Note: `Add` is intentionally synchronous — it only queues the entity in the EF change
tracker; the actual INSERT happens when `IReadModelUnitOfWork.SaveChangesAsync` is called.

### Step 4 — EF configuration in `Miniclip.Simulator.Infrastructure.Read`

```csharp
// Persistence/Configurations/ProcessedEventsConfiguration.cs
public class ProcessedEventsConfiguration : IEntityTypeConfiguration<ProcessedEventModel>
{
    public void Configure(EntityTypeBuilder<ProcessedEventModel> builder)
    {
        builder.ToTable("ProcessedEvents");
        builder.HasKey(x => new { x.EventId, x.ConsumerGroup });
        builder.Property(x => x.EventId).HasMaxLength(36).IsRequired();
        builder.Property(x => x.ConsumerGroup).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProcessedAt).IsRequired();
    }
}
```

```csharp
// Persistence/Repositories/Write/ProcessedEventsRepository.cs
public class ProcessedEventsRepository(SimulatorReadDbContext context) : IProcessedEventsRepository
{
    public async Task<bool> ContainsAsync(
        string eventId,
        string consumerGroup,
        CancellationToken cancellationToken)
        => await context.Set<ProcessedEventModel>()
            .AnyAsync(e => e.EventId == eventId && e.ConsumerGroup == consumerGroup, cancellationToken);

    public void Add(string eventId, string consumerGroup)
        => context.Set<ProcessedEventModel>().Add(
            new ProcessedEventModel { EventId = eventId, ConsumerGroup = consumerGroup });
}
```

`SimulatorReadDbContext` picks up `ProcessedEventsConfiguration` automatically via
`ApplyConfigurationsFromAssembly` — no changes to the context class are required.

### Step 5 — Add EF Core migration

```powershell
dotnet ef migrations add AddProcessedEvents `
  --project src/Miniclip.Simulator.Infrastructure.Read `
  --startup-project src/Miniclip.Simulator.Api
```

Verify the generated migration creates the `ProcessedEvents` table with the composite PK.

### Step 6 — Add `ProjectionsConsumerService<TEvent>` to `Miniclip.Simulator.ReadModels.Projections`

```csharp
// ProjectionsConsumerService.cs
public class ProjectionsConsumerService<TEvent>(
    IEventSerializer serializer,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ProjectionsConsumerService<TEvent>> logger)
    : KafkaConsumerService([TopicNaming.ForType<TEvent>()], configuration, logger)
    where TEvent : IDomainEvent
{
    protected override string ConsumerGroupId => "simulator-projections";

    protected override async Task HandleAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken)
    {
        var eventId = result.GetHeader("event-id");
        var eventType = result.GetHeader("event-type");
        var domainEvent = serializer.Deserialize(eventType, result.Message.Value);

        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var processedEvents = sp.GetRequiredService<IProcessedEventsRepository>();
        if (await processedEvents.ContainsAsync(eventId, ConsumerGroupId, cancellationToken))
            return;

        var uow = sp.GetRequiredService<IReadModelUnitOfWork>();
        var publisher = sp.GetRequiredService<IPublisher>();

        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            await publisher.Publish(domainEvent, cancellationToken);
            processedEvents.Add(eventId, ConsumerGroupId);
            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

`TEvent` is used only to compute the topic string at construction time.
Everything inside `HandleAsync` operates on `IDomainEvent` — adding a consumer for
`GroupCreated` in Phase 5 requires no changes to this class.

### Step 7 — Remove `MatchPlayedKafkaRelayService`

Delete `Miniclip.Simulator.ReadModels.Projections/MatchPlayedKafkaRelayService.cs`.

### Step 8 — Update DI in `DatabaseConfiguration`

Register `IProcessedEventsRepository` alongside the other write repositories, and swap the
hosted service:

```csharp
// Kafka
services.AddKafkaEventBus(kafkaBootstrapServers);
services.AddHostedService<ProjectionsConsumerService<MatchPlayed>>();  // replaces MatchPlayedKafkaRelayService

// Read model repositories (Write)
// ... existing registrations ...
services.AddScoped<IProcessedEventsRepository>(sp =>
    new ProcessedEventsRepository(sp.GetRequiredService<SimulatorReadDbContext>()));
```

When a new event type gains projection handlers in Phase 5+, adding its consumer is one line:

```csharp
services.AddHostedService<ProjectionsConsumerService<GroupCreated>>();
```

Remove the `using` for `MatchPlayedKafkaRelayService` from the file; add usings for
`ProjectionsConsumerService` and `IProcessedEventsRepository` / `ProcessedEventsRepository`.

### Step 9 — Update unit tests

The existing projection unit tests (`WhenProjectingGroupStandings`, `WhenProjectingMatchResults`,
`WhenRecalculatingPosition`) test handlers directly and are **unaffected** by this change.

Add unit tests for `ProjectionsConsumerService<MatchPlayed>` covering:

| Test case | Expected behaviour |
|---|---|
| Message not previously processed | Dispatches via Mediator, saves, commits, records as processed |
| Message already in `ProcessedEvents` | Returns immediately; no UoW calls made |
| Mediator throws | Rollback called; exception re-thrown (offset not committed) |

---

## Scope Dependency Graph (after Phase 4)

```
IServiceScope (per Kafka message)
  ├─ SimulatorReadDbContext          ← single instance shared across all below
  ├─ IReadModelUnitOfWork            ← wraps context transaction
  ├─ IPublisher (Mediator)
  │    ├─ MatchResultProjection(IMatchResultsRepository)
  │    └─ GroupStandingsProjection(IGroupStandingsRepository, IRecalculatePositionService)
  └─ IProcessedEventsRepository      ← writes to same context
```

All EF operations within the scope share the same `SimulatorReadDbContext` instance →
`SaveChangesAsync` flushes projections + `ProcessedEvents` insert in one atomic write,
wrapped in a single MySQL transaction.

`ProjectionsConsumerService<GroupCreated>` would use an identical scope — the only
difference is the topic it subscribes to and which `INotificationHandler<GroupCreated>`
implementations Mediator dispatches to.

---

## Definition of Done

- [x] `ProcessedEvents` table exists in MySQL (via EF migration)
- [x] `ProjectionsConsumerService<TEvent>` compiles and is registered as `ProjectionsConsumerService<MatchPlayed>`
- [x] `MatchPlayedKafkaRelayService.cs` is deleted
- [x] Projection writes are flushed to MySQL inside a `IReadModelUnitOfWork` transaction
- [x] A re-delivered Kafka message (same `event-id`) is skipped without side effects
- [x] All existing unit tests pass
- [x] Build is green

---

## Out of Scope (deferred to Phase 5)

| Concern | Deferred To |
|---|---|
| Disabling Kafka topic auto-creation; pre-declaring topics | Phase 5 |
| Dead-letter topic handling for failed messages | Phase 5 |
| Outbox pattern to guarantee at-least-once ESDB→Kafka delivery | Phase 5 |
| Consumers for `GroupCreated`, `TeamAdded`, `MatchScheduled` (no handlers yet) | Phase 5 |
| Integration tests covering the full Kafka round-trip | Phase 5 |

---

## On Completion

When this phase is done, update:

- `docs/event-sourcing/PLAN.md`
- `AI.md`
- `.github/copilot-instructions.md`

Mark Phase 4 as complete and move the current phase to Phase 5.
