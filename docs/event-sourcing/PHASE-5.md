# Phase 5 — Testing & Hardening

> **Status:** ✅ Done
> **Branch:** `feat/phase-5-hardening`
> **Depends on:** Phase 4 complete ✅

---

## Goal

Harden the Kafka consumer pipeline for production readiness and add a projection integration
test project that verifies the full projection seam — from `MatchPlayed` notification to
persisted read model rows — without requiring a running Kafka broker.

---

## Issues Addressed

`ProjectionsConsumerService<MatchPlayed>` had four correctness issues that were fixed in this phase:

1. **No retry logic** — a transient failure (e.g. DB timeout) causes the message offset to
   not be committed. The consumer logs and continues, leaving the message to be re-read on
   restart. There is no delay, no attempt counter, and no dead-letter path. A permanently
   bad message (poison pill) will block the consumer indefinitely after every restart.

2. **Deserialisation before idempotency check** — `serializer.Deserialize` is called
   before `processedEvents.ContainsAsync`. For a duplicate message (already processed),
   the deserialization cost is paid unnecessarily.

3. **Hardcoded consumer group ID** — `ConsumerGroupId` returns `"simulator-projections"`
   for all `TEvent` specialisations. Registering a second consumer (e.g.
   `ProjectionsConsumerService<TeamAdded>`) would cause Kafka to treat both instances as
   the same consumer group on different topics, distributing partitions between them instead
   of each receiving all events from its own topic independently.

4. **`ReadModelUnitOfWorkBehavior` still in the write pipeline** — since Phase 4 moved all
   read model saves to the Kafka consumer, this behavior now opens a MySQL transaction and
   calls `SaveChanges` on an empty change tracker on every write command: dead weight.

---

## Workstream A — Consumer Resilience (`Miniclip.Core.Kafka`) ✅

### Problem

The current inner loop in `KafkaConsumerService.ExecuteAsync`:

```
try
  Consume → HandleAsync → Commit
catch (OperationCanceledException) → break
catch (Exception ex)               → log and continue (offset NOT committed)
```

A permanently failing message re-blocks the consumer on every restart. There is no bound on
the number of retries and no mechanism to retire a poison pill.

### Design

Add a retry loop around `HandleAsync`. Introduce `IConsumerRetryPolicy` to make retry
behaviour injectable and testable:

```csharp
// Miniclip.Core.Kafka/IConsumerRetryPolicy.cs
public interface IConsumerRetryPolicy
{
    int MaxAttempts { get; }
    TimeSpan Delay(int attempt); // attempt is 1-based
}
```

Default implementation:

```csharp
// Miniclip.Core.Kafka/ExponentialBackoffRetryPolicy.cs
public sealed class ExponentialBackoffRetryPolicy(int maxAttempts = 3, TimeSpan? baseDelay = null)
    : IConsumerRetryPolicy
{
    private readonly TimeSpan _base = baseDelay ?? TimeSpan.FromSeconds(1);
    public int MaxAttempts { get; } = maxAttempts;
    public TimeSpan Delay(int attempt) => _base * Math.Pow(2, attempt - 1);
}
```

`KafkaConsumerService` receives an optional `IConsumerRetryPolicy` (defaults to
`ExponentialBackoffRetryPolicy`) and adds a virtual dead-letter hook:

```
while (not stopping)
  result = Consume()
  attempts = 0
  while (true)
    try
      HandleAsync(result)
      Commit(result)
      break
    catch OperationCanceledException → rethrow
    catch Exception when attempts < MaxAttempts
      attempts++
      log warning
      await Task.Delay(policy.Delay(attempts))
    catch Exception (exhausted)
      log error
      await OnDeadLetterAsync(result, ex)   ← virtual hook
      Commit(result)                         ← advance past the poison pill
      break
```

`OnDeadLetterAsync` has a default no-op implementation in the base class. Derived classes
that need to write to a dead-letter Kafka topic can override it.

### Projects Affected

| Project | Change |
|---|---|
| `Miniclip.Core.Kafka` | Add `IConsumerRetryPolicy`, `ExponentialBackoffRetryPolicy`; update `KafkaConsumerService` |
| `Miniclip.Core.Kafka` *(tests — new project)* | Unit tests for retry loop logic |

### Definition of Done

- [x] Transient failure retried up to `MaxAttempts` with `policy.Delay(attempt)` backoff
- [x] After all retries exhausted, `OnDeadLetterAsync` is called and the offset is committed
- [x] `OperationCanceledException` is never retried — still breaks the loop immediately
- [x] Default policy is `ExponentialBackoffRetryPolicy(maxAttempts: 3, baseDelay: 1 s)`
- [x] Unit tests cover: success on first try, success on retry N, exhausted retries, cancellation

---

## Workstream B — Consumer Correctness (`Miniclip.Simulator.ReadModels.Projections`) ✅

### Problem 1:

```csharp
// Current
var domainEvent = serializer.Deserialize(eventType, result.Message.Value); // ← always
if (await processedEvents.ContainsAsync(eventId, ...))
    return;
```

For a duplicate message the payload is deserialised, an EF scope is created, and a DB read
is made before the early-return. Only the `event-id` header is needed for the check.

**Fix:** move the scope creation and idempotency check to the top of `HandleAsync`, before
reading `event-type` or calling `Deserialize`.

### Problem 2: Shared consumer group ID

**Fix:** derive the group ID from `TEvent` using the same `TopicNaming` convention already
used for the topic name:

```csharp
protected override string ConsumerGroupId
    => $"simulator-projections-{TopicNaming.ForType<TEvent>().Replace("simulator.", string.Empty)}";
// MatchPlayed → "simulator-projections-match-played"
// TeamAdded   → "simulator-projections-team-added"
```

> **Deployment note:** existing committed offsets are stored under `"simulator-projections"`.
> After deployment, the consumer restarts from `AutoOffsetReset.Earliest`. The
> `ProcessedEvents` idempotency guard ensures that re-delivered events already in the table
> are skipped without re-projecting.

### Unit test update

`AndMessageAlreadyProcessed` must gain one assertion confirming that deserialization is
skipped for duplicate messages:

```csharp
[Test]
public void ShouldNotDeserializePayload()
    => Serializer.DidNotReceive().Deserialize(Arg.Any<string>(), Arg.Any<byte[]>());
```

### Projects Affected

| Project | Change |
|---|---|
| `Miniclip.Simulator.ReadModels.Projections` | Reorder `HandleAsync`; update `ConsumerGroupId` |
| `Miniclip.Simulator.ReadModels.Projections.UnitTests` | Add assertion to `AndMessageAlreadyProcessed` |

### Definition of Done

- [x] `processedEvents.ContainsAsync` is called before `serializer.Deserialize`
- [x] `ConsumerGroupId` returns `"simulator-projections-group"` for `TEvent = MatchPlayedIntegrationEvent` (derived from aggregate type, not event type)
- [x] `AndMessageAlreadyProcessed.ShouldNotDeserializePayload` test exists and passes
- [x] All 14 consumer unit tests green

---

## Workstream C — Write-Pipeline Cleanup (`Miniclip.Simulator.Api`) ✅

### Problem

`ReadModelUnitOfWorkBehavior` is still registered as the outermost write-pipeline behavior:

```
ReadModelUnitOfWorkBehavior     ← BeginTransaction (MySQL)
  EventStoreCommandBehavior     ← IEventStoreSession.CommitAsync → KurrentDB
    CommandHandler
  → ICommittedEventPublisher.PublishAsync → IEventBus (Kafka)
  → SaveChanges  (no pending changes — no-op)
  → Commit       (empty transaction)
```

Since Phase 3, `EventStoreCommandBehavior` publishes to Kafka via `ICommittedEventPublisher` — not to the in-process
Mediator — so no projection handler runs during the HTTP request. Since Phase 4, the Kafka
consumer manages its own `IReadModelUnitOfWork` transaction. The behavior adds two unnecessary
MySQL roundtrips per command: `BeginTransaction` and `Commit`.

The class itself is correct and should not be deleted — it may serve future use cases where
synchronous read model updates are needed. Only the registration is removed.

Command handler unit tests (`WhenGeneratingGroups`, `WhenSimulatingGroups`) test the handler
directly via `AsyncTestBase<THandler>` and do not reference `IReadModelUnitOfWork`, so no
test changes are needed.

### Projects Affected

| Project | Change |
|---|---|
| `Miniclip.Simulator.Api` | Remove `AddScoped<IPipelineBehavior<,>, ReadModelUnitOfWorkBehavior<,>>` from `MediatorConfiguration` |

### Definition of Done

- [x] `ReadModelUnitOfWorkBehavior<,>` not registered in `MediatorConfiguration`
- [x] All existing command handler tests pass

---

## Workstream D — Projection Integration Tests ✅

### Problem

The projection handlers (`GroupStandingsProjection`, `MatchResultProjection`,
`RecalculatePositionService`) are tested in isolation with mocked repositories. There is no
test verifying that dispatching a `MatchPlayed` notification through a **real** Mediator
produces the correct rows in the database via real EF Core repositories.

The Kafka consumer unit tests verify the consumer *infrastructure* (idempotency, UoW
wiring, error handling) but use a mocked `IPublisher`. There is a gap between "IPublisher
was called" and "the correct data ended up in the read model".

### Design

**New project:** `Miniclip.Simulator.ReadModels.Projections.IntegrationTests`

**Stack:**
- `ServiceCollection` wired with the same registrations as production, minus the read-only
  query repositories
- `SimulatorReadDbContext` using `UseInMemoryDatabase` — no external server required
- Real `Mediator` with real `OrderedNotificationPublisher` and real projection handlers
- Real `RecalculatePositionService`
- Real write-side repositories (`GroupStandingsRepository`, `MatchResultsRepository`)

**Scope — what this tests:**
- The correct `MatchResultModel` row is written after `MatchPlayed` is published
- The correct `GroupStandingsModel` rows are created/updated after `MatchPlayed`
- Position recalculation is correct across multiple matches within the same group

**Scope — what this does NOT test:**
- Kafka delivery (covered by `WhenConsumingMatchPlayed` unit tests)
- Idempotency (covered by `WhenConsumingMatchPlayed` unit tests)
- EF migrations or MySQL-specific DDL (covered by the running application)

**Test structure:**

```
WhenAMatchIsPlayed/
  WhenAMatchIsPlayed.cs            ← base: wires DI, builds MatchPlayed, calls publisher.Publish
  WithFirstMatchInGroup.cs         ← asserts MatchResult row + initial GroupStandings rows
  WithSubsequentMatchSameGroup.cs  ← asserts standings updated; positions recalculated
```

**DI wiring pattern (base class):**

```csharp
protected override void Given()
{
    var services = new ServiceCollection();
    services.AddDbContext<SimulatorReadDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
    services.AddMediator(o => o.ServiceLifetime = ServiceLifetime.Scoped);
    services.AddSingleton<INotificationPublisher, OrderedNotificationPublisher>();
    // real projection handlers and services
    ...
    ServiceProvider = services.BuildServiceProvider();
}

protected override async ValueTask WhenAsync()
{
    using var scope = ServiceProvider.CreateScope();
    var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
    await publisher.Publish(Event, CancellationToken.None);
    // wrap in uow so SaveChanges is called
}
```

### Projects Affected

| Project | Change |
|---|---|
| `Miniclip.Simulator.ReadModels.Projections.IntegrationTests` *(new)* | New project; reference `Microsoft.EntityFrameworkCore.InMemory` |

### Definition of Done

- [x] `WithFirstMatchInGroup` — one `MatchResultModel` row with correct team IDs and scores
- [x] `WithFirstMatchInGroup` — two `GroupStandingsModel` rows with correct W/D/L/GF/GA
- [x] `WithSubsequentMatchSameGroup` — positions recalculated correctly after two matches

---

## Additional Deliverables (Beyond Original Scope)

These items were implemented as part of Phase 5 but were not in the original workstream spec.

### `KafkaConsumerService` Refactor

`KafkaConsumerService` was made abstract; `IConsumer<string,byte[]>` is now built internally
via `BuildConsumer(ConsumerConfig)` (abstract) and `ConsumerGroupId` (abstract property).
`IConfiguration` is injected to build the consumer config at startup. This eliminates the
factory lambda in `AddHostedService` that previously required scoped services to be resolved
from the root provider.

### Configuration Split

`DatabaseConfiguration` was decomposed into three focused extension methods:

| Class | Concern |
|---|---|
| `ReadModelsConfiguration` | `SimulatorReadDbContext`, read/write repositories, `IReadModelUnitOfWork` |
| `EventStoreDbConfiguration` | EventStoreDB client, `IAggregateRepository<T>`, `TeamDataSeeder` |
| `KafkaConfiguration` | `KafkaEventBus`, `ProjectionsConsumerService<MatchPlayed>` |

### Team → EventStoreDB Migration

`Team` aggregate moved from MySQL to EventStoreDB:

- `TeamRegistered` domain event introduced
- `Team.cs` updated with private parameterless constructor, `Apply(IDomainEvent)`, and `Create` that sets state directly and enqueues `TeamRegistered`
- `GetAllAsync` implemented across the full abstraction stack using the `$ce-team` category stream
- `TeamDataSeeder` (`IHostedService`) seeds 10 fixed-GUID teams on startup; idempotent via `FindAsync`
- EF migration `20260322000000_DropLegacyTables.cs` drops all legacy write-side tables (Groups, GroupTeams, Matches, Teams)

---

## Implementation Order

| # | Workstream | Rationale |
|---|---|---|
| 1 | **B** — Consumer Correctness | Smallest change; fixes bugs; updates unit tests that serve as baseline |
| 2 | **C** — Write-Pipeline Cleanup | One-line removal; confirm all tests still pass |
| 3 | **A** — Consumer Resilience | New abstractions in `Core.Kafka`; isolated from simulator-specific code |
| 4 | **D** — Integration Tests | New project; validates the full projection seam |

---

## Projects Affected Summary

| Project | Workstream | Change |
|---|---|---|
| `Miniclip.Core.Kafka` | A + Extra | `IConsumerRetryPolicy`, `ExponentialBackoffRetryPolicy`; abstract `BuildConsumer`/`ConsumerGroupId`; retry loop with `OnDeadLetterAsync` |
| `Miniclip.Core.Kafka.UnitTests` | A | Retry loop unit tests |
| `Miniclip.Simulator.ReadModels.Projections` | B + Extra | Reordered `HandleAsync`; per-type `ConsumerGroupId`; `IServiceScopeFactory` per-message scoping |
| `Miniclip.Simulator.ReadModels.Projections.UnitTests` | B | `ShouldNotDeserializePayload` assertion added to `AndMessageAlreadyProcessed` |
| `Miniclip.Simulator.Api` | C + Extra | Removed `ReadModelUnitOfWorkBehavior` registration; config split into `ReadModelsConfiguration`, `EventStoreDbConfiguration`, `KafkaConfiguration` |
| `Miniclip.Simulator.ReadModels.Projections.IntegrationTests` | D | New integration test project |
| `Miniclip.Simulator.Domain` | Extra | `TeamRegistered` event; `Team.cs` made event-sourced |
| `Miniclip.Simulator.Infrastructure.Write` | Extra | EF migration dropping all legacy write-side aggregate tables |
