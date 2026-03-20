# Phase 2 - EventStoreDB: Write Side Migration

> **Status:** ⬜ Pending
> **Branch:** `feat/phase-2-esdb-write`
> **Depends on:** Phase 1 complete
> **Must not break:** all existing API behaviour and tests must remain green during the transition

---

## Goal

Move the write side from EF Core aggregate persistence to **EventStoreDB**.
The write model will persist **domain events** instead of aggregate state.
The read model remains on MySQL and is untouched in this phase.

---

## Current Write-Side Baseline (as-is)

| Area | Current State |
|---|---|
| `Group` | Private parameterised constructor `Group(Guid id, string name, int capacity)`; `Create`, `AddTeam`, `AddMatch`, `SimulateMatch`; only `SimulateMatch` calls `Enqueue(new MatchPlayed(...))`. |
| `Match` | Private parameterless constructor already present; `Create` factory; `SimulateResult` with validation; owned by `Group`. |
| `MatchPlayed` | Rich `record MatchPlayed(...) : IDomainEvent` carrying all denormalised match data (GroupId, GroupName, MatchId, HomeTeamId, HomeTeamName, HomeTeamStrength, HomeScore, AwayTeamId, AwayTeamName, AwayTeamStrength, AwayScore, Round). Only domain event in the system. |
| `GenerateGroupCommandHandler` | Calls `Group.Create`, adds teams, generates fixtures via `fixtureSchedulerService`, calls `.Tap(groupsRepository.Add)`. |
| `SimulateGroupCommandHandler` | Calls `repository.FindAsync(command.GroupId)`, then `groupSimulator.SimulateAllMatches(group)`. Does **not** explicitly save. |
| `GroupsRepository` | EF Core; `FindAsync` uses `.Include(Matches).ThenInclude(HomeTeam / AwayTeam)` to reconstruct `Group` in memory. |
| `SimulatorUnitOfWork` | EF Core; `SaveChangesAsync`, `BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`, `GetTrackedAggregates()`. |
| `CommandUnitOfWorkBehavior` | Wraps every command in `BeginTransaction -> next -> SaveChanges + Commit or Rollback`. |
| `DatabaseConfiguration` | Registers `IRepository<Group>` -> `GroupsRepository` and `IUnitOfWork` -> `SimulatorUnitOfWork`, both backed by `SimulatorWriteDbContext`. Also registers `IRepository<Team>` against the same write context. |
| `MediatorConfiguration` | Pipeline order: `CommandUnitOfWorkBehavior` -> `ReadModelUnitOfWorkBehavior` -> `DomainEventPublisherBehavior`. |
| `IEventStore<T>` | Already implemented: `AppendAsync` (appends uncommitted events, updates `Version`), `LoadAsync` (replays stream, returns null when stream not found). |

---

## Critical Design Gap

The current aggregate only emits events in `SimulateMatch`. `Group.Create`, `AddTeam`, and `AddMatch` are **silent** — they mutate state without raising any events.

This makes event-sourced replay impossible for the write side: `EventStoreDbEventStore.LoadAsync` replays a stream to reconstruct aggregate state, but after a `GenerateGroup` command the stream would be **empty** (no events were appended), and `AppendAsync` exits early when `DequeueUncommittedEvents()` returns an empty array.

Additionally, `SimulateGroupCommandHandler` relies on `Group` having fully populated `Match` objects with `HomeTeam` and `AwayTeam` entity references (including `Strength`) to run the simulation. A `Group` replayed only from `MatchPlayed` events would not have those references for **unplayed** matches.

**These two gaps must be closed in Phase 2 before any infrastructure change makes sense.**

---

## Proposed Design

### 1. Add three new domain events to close the replay gap

| New Event | Emitted From | Purpose |
|---|---|---|
| `GroupCreated(GroupId, Name, Capacity)` | `Group.Create` | Records group identity and configuration. |
| `TeamAdded(GroupId, TeamId, Name, Strength)` | `Group.AddTeam` | Records which team was added, with enough data to reconstruct the `Team` entity for simulation. |
| `MatchScheduled(GroupId, MatchId, HomeTeamId, AwayTeamId, Round)` | `Group.AddMatch` | Records which fixture was scheduled. Team data is already known from `TeamAdded` events. |

Together with the existing `MatchPlayed`, these four events capture the full lifecycle of a `Group` and make complete replay possible.

### 2. Add a private parameterless constructor and `Apply` overrides to `Group`

`EventStoreDbEventStore.LoadAsync` uses `Activator.CreateInstance(typeof(T), nonPublic: true)` to create the shell aggregate, then calls `ReplayEvent(event, version)` for each stored event. `Group` needs:

- `private Group()` — initialises `teams` and `matches` lists (parameterless, for replay only)
- `protected override void Apply(IDomainEvent @event)` — dispatches to a private handler per event type
- `private void Apply(GroupCreated e)` — sets `Id`, `Name`, `Capacity`
- `private void Apply(TeamAdded e)` — reconstructs and adds a `Team` to the internal list
- `private void Apply(MatchScheduled e)` — reconstructs and adds a `Match` to the internal list using team references already in the list
- `private void Apply(MatchPlayed e)` — finds the match by Id and calls `match.ApplyResult(e.HomeScore, e.AwayScore)`

### 3. Add `Match.ApplyResult` for replay

`Match.SimulateResult` validates inputs (no negative scores, not already played) before mutating state. During replay, the event is already valid and must be applied without failure. Add:

`internal void ApplyResult(int homeScore, int awayScore)` — sets `HomeScore`, `AwayScore`, `IsPlayed = true` with no validation.

### 4. Keep `IRepository<Group>` — back it with an event-sourced implementation

Command handlers already depend on `IRepository<Group>` via constructor injection. Keeping the interface avoids touching the handlers. The new `EventSourcedGroupsRepository` implements it and also implements a new `IFlushable` interface.

`IRepository<Group>.Add` is synchronous (it is called via `.Tap(...)` in the handler). The repository stores the aggregate in a scoped field. A command pipeline behavior flushes it to EventStoreDB after the handler succeeds.

`IRepository<Group>.FindAsync` calls `eventStore.LoadAsync` and caches the result in the same scoped field so the post-command flush appends only new events.

### 5. Introduce `IFlushable` and `EventStoreCommandBehavior`

`IFlushable` is a single-method interface:

```csharp
public interface IFlushable
{
    Task FlushAsync(CancellationToken cancellationToken = default);
}
```

`EventStoreCommandBehavior<TRequest, TResponse>` replaces `CommandUnitOfWorkBehavior` in the mediator pipeline. It:

- skips non-command requests (using the existing `IsCommand()` extension)
- calls `next(request, cancellationToken)`
- if the response `IsSuccessful()`, calls `FlushAsync` on all registered `IFlushable` instances
- on exception or failure, does nothing (EventStoreDB append was never called)

### 6. Handle the `Team` repository after write context removal

`IRepository<Team>` is currently registered against `SimulatorWriteDbContext`. Teams are read-only reference data. When the write context is removed, `IRepository<Team>` must be re-registered against a context that still has a `Team` table. Two options:

| Option | Pros | Cons |
|---|---|---|
| Re-register `IRepository<Team>` against `SimulatorReadDbContext` | No new context needed; teams already exist in the read DB | Mixes read and command-side concerns slightly |
| Keep a minimal `SimulatorTeamDbContext` for teams only | Clean separation | Extra context to maintain |


---

**Decision:** Defer to implementation. Prefer re-using the read context for teams and document the rationale in the code.

---

## Scope of Phase 2

### In scope

- Three new domain events: `GroupCreated`, `TeamAdded`, `MatchScheduled`
- `Group`: private parameterless constructor, `Apply` overrides for all four events
- `Match`: `ApplyResult` for replay without validation
- `IFlushable` interface
- `EventSourcedGroupsRepository` (implements `IRepository<Group>` and `IFlushable`)
- `EventStoreCommandBehavior` replacing `CommandUnitOfWorkBehavior`
- DI wiring update in `DatabaseConfiguration` and `MediatorConfiguration`
- Removal of `SimulatorUnitOfWork`, `SimulatorWriteDbContext`, EF `GroupsRepository`, `GroupConfiguration`, EF migrations for the write side
- Write-side unit test updates

### Out of scope

- Kafka integration
- Read model consumer migration
- Snapshotting
- Dead-letter handling
- `Team` event sourcing

---

## Planned Projects Affected

| Project | Planned Change |
|---|---|
| `Miniclip.Simulator.Domain` | Add `GroupCreated`, `TeamAdded`, `MatchScheduled` events; emit them from `Group.Create`, `AddTeam`, `AddMatch`; add private parameterless constructor and `Apply` overrides to `Group`; add `Match.ApplyResult`. |
| `Miniclip.Core.Domain` | Add `IFlushable` interface. |
| `Miniclip.Core.Application` | Add `EventStoreCommandBehavior<TRequest, TResponse>`; keep or remove `CommandUnitOfWorkBehavior` (becomes dead code). |
| `Miniclip.Simulator.Infrastructure.Write` | Add `EventSourcedGroupsRepository`; delete EF `GroupsRepository`, `SimulatorUnitOfWork`, `SimulatorWriteDbContext`, `GroupConfiguration`, and migrations. |
| `Miniclip.Simulator.Api` | Update `DatabaseConfiguration` and `MediatorConfiguration` for event-sourced wiring. |
| `Miniclip.Simulator.Application.Commands.UnitTests` | Update mocks and assertions for all generation and simulation tests. |

---

## Implementation Plan

### Step 1 — Add new domain events

Create three event records in `Miniclip.Simulator.Domain/Aggregates/Groups/Events/`:

```csharp
public record GroupCreated(Guid GroupId, string Name, int Capacity) : IDomainEvent;

public record TeamAdded(Guid GroupId, Guid TeamId, string Name, int Strength) : IDomainEvent;

public record MatchScheduled(
    Guid GroupId,
    Guid MatchId,
    Guid HomeTeamId,
    Guid AwayTeamId,
    int Round
) : IDomainEvent;
```

Emit them from the aggregate:

- `Group.Create`: replace `return new Group(id, name, capacity)` with a factory that creates the group **and** calls `Enqueue(new GroupCreated(id, name, capacity))`
- `Group.AddTeam`: call `Enqueue(new TeamAdded(Id, team.Id, team.Name, team.Strength))` before returning `Result.Success()`
- `Group.AddMatch`: call `Enqueue(new MatchScheduled(Id, id, homeTeam.Id, awayTeam.Id, round))` after the match is added

> Note: `Enqueue` is called **after** state mutation succeeds so that events are only raised for valid transitions.

### Step 2 — Make `Group` replayable

In `Group.cs`:

```csharp
// For EventStoreDB replay — not for application use
private Group()
{
    teams = [];
    matches = [];
}

protected override void Apply(IDomainEvent @event)
{
    switch (@event)
    {
        case GroupCreated e: Apply(e); break;
        case TeamAdded e:    Apply(e); break;
        case MatchScheduled e: Apply(e); break;
        case MatchPlayed e:  Apply(e); break;
    }
}

private void Apply(GroupCreated e) { /* set Id, Name, Capacity via backing fields or init */ }
private void Apply(TeamAdded e)    { teams.Add(Team.Restore(e.TeamId, e.Name, e.Strength)); }
private void Apply(MatchScheduled e)
{
    var home = teams.First(t => t.Id == e.HomeTeamId);
    var away = teams.First(t => t.Id == e.AwayTeamId);
    matches.Add(Match.Restore(e.MatchId, home, away, e.Round));
}
private void Apply(MatchPlayed e)
{
    matches.First(m => m.Id == e.MatchId).ApplyResult(e.HomeScore, e.AwayScore);
}
```

`Group.Name` and `Group.Capacity` are `{ get; }` (immutable). Change them to `{ get; private set; }` to allow replay assignment, or add `init` setters.

`Team.Restore` and `Match.Restore` are internal static factory methods that bypass validation for replay:

- `Team.Restore(Guid id, string name, int strength)` — creates a `Team` directly without validation
- `Match.Restore(Guid id, Team home, Team away, int round)` — creates a `Match` without the same-team guard

### Step 3 — Add `Match.ApplyResult`

```csharp
internal void ApplyResult(int homeScore, int awayScore)
{
    HomeScore = homeScore;
    AwayScore = awayScore;
    IsPlayed = true;
}
```

### Step 4 — Add `IFlushable`

Create in `Miniclip.Core.Domain`:

```csharp
namespace Miniclip.Core.Domain;

public interface IFlushable
{
    Task FlushAsync(CancellationToken cancellationToken = default);
}
```

### Step 5 — Create `EventSourcedGroupsRepository`

Create in `Miniclip.Simulator.Infrastructure.Write/Persistence/Repositories/`:

```csharp
public class EventSourcedGroupsRepository(IEventStore<Group> eventStore)
    : IRepository<Group>, IFlushable
{
    private Group? _tracked;

    public void Add(Group aggregate) => _tracked = aggregate;

    public async Task<Group?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        _tracked = await eventStore.LoadAsync(id, cancellationToken);
        return _tracked;
    }

    public Task<IEnumerable<Group>> GetAllAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException("GetAllAsync is not supported for event-sourced repositories.");

    public void Delete(Group aggregate)
        => throw new NotSupportedException("Delete is not supported for event-sourced repositories.");

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_tracked is not null)
            await eventStore.AppendAsync(_tracked, cancellationToken);
    }
}
```

### Step 6 — Add `EventStoreCommandBehavior`

Create in `Miniclip.Core.Application/Behaviors/`:

```csharp
public class EventStoreCommandBehavior<TRequest, TResponse>(
    IEnumerable<IFlushable> flushables)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!request.IsCommand())
            return await next(request, cancellationToken);

        var response = await next(request, cancellationToken);

        if (response.IsSuccessful())
            foreach (var flushable in flushables)
                await flushable.FlushAsync(cancellationToken);

        return response;
    }
}
```

Note: `EventStoreDb` appends are idempotent per stream revision. If the handler fails partway through, no append was made, so there is nothing to roll back.

### Step 7 — Update DI wiring

In `DatabaseConfiguration`:

- Remove `SimulatorWriteDbContext` registration
- Remove `IRepository<Group>` -> `GroupsRepository`
- Remove `IUnitOfWork` -> `SimulatorUnitOfWork`
- Register `IEventStore<Group>` — already covered by the open-generic `IEventStore<>` registration from `AddEventStoreDbClient`
- Register `EventSourcedGroupsRepository` as **scoped** for both `IRepository<Group>` and `IFlushable`:

```csharp
services.AddScoped<EventSourcedGroupsRepository>();
services.AddScoped<IRepository<Group>>(sp => sp.GetRequiredService<EventSourcedGroupsRepository>());
services.AddScoped<IFlushable>(sp => sp.GetRequiredService<EventSourcedGroupsRepository>());
```

- Re-register `IRepository<Team>` against `SimulatorReadDbContext` (teams are read-only reference data)
- Call `AddEventStoreDbClient(connectionString)` with the EventStoreDB connection string from configuration

In `MediatorConfiguration`:

- Replace `CommandUnitOfWorkBehavior<,>` with `EventStoreCommandBehavior<,>`
- Remove `IUnitOfWork` pipeline references

### Step 8 — Remove EF write infrastructure

Delete from `Miniclip.Simulator.Infrastructure.Write`:

- `Persistence/SimulatorWriteDbContext.cs`
- `Persistence/SimulatorUnitOfWork.cs`
- `Persistence/Repositories/GroupsRepository.cs`
- `Persistence/Configurations/GroupConfiguration.cs`
- `Migrations/` folder (all migration files)

Verify that `TeamConfiguration.cs` can also be removed or moved. If `IRepository<Team>` is re-registered against the read context, `TeamConfiguration` in the write project becomes dead code.

Update `DatabaseConfiguration.InitializeDatabases` to remove the write context migration call.

### Step 9 — Update command tests

**`WhenGeneratingGroups`:**

- `GroupRepository` mock no longer needs a `FindAsync` setup
- `GroupRepository.Add` assertion remains (the call is unchanged)
- Add an `IFlushable` mock (or mock `IEventStore<Group>`) and assert `FlushAsync` is called once after a successful command
- Since `Group.Create`, `AddTeam`, and `AddMatch` now enqueue events, `DequeueUncommittedEvents()` in the `Add` flow will have items — no change needed in the handler test, but may need updating in domain unit tests

**`WhenSimulatingGroups`:**

- `GroupRepository.FindAsync` mock still returns a `Group` built via `GroupMother` — this remains valid because the test constructs the group in-memory
- Add assertion that `FlushAsync` is called once after simulation
- Add a test for the case where `FindAsync` returns `null` (stream does not exist in EventStoreDB)

**New test: concurrency conflict:**

- Mock `IEventStore<Group>.AppendAsync` to throw `WrongExpectedVersionException`
- Assert the command returns a failure result with a conflict message

---

## Definition of Done

- [ ] `GroupCreated`, `TeamAdded`, and `MatchScheduled` events exist and are emitted from the aggregate
- [ ] `Group` has a private parameterless constructor and `Apply` overrides for all four event types
- [ ] `Match` has `ApplyResult` for replay
- [ ] `Team` has a `Restore` factory for replay
- [ ] `IFlushable` is defined in `Miniclip.Core.Domain`
- [ ] `EventSourcedGroupsRepository` is the registered `IRepository<Group>` in production
- [ ] `EventStoreCommandBehavior` is in the mediator pipeline instead of `CommandUnitOfWorkBehavior`
- [ ] `GenerateGroupCommand` appends events to EventStoreDB
- [ ] `SimulateGroupCommand` loads the group from EventStoreDB and appends new `MatchPlayed` events
- [ ] `SimulatorUnitOfWork`, `SimulatorWriteDbContext`, EF `GroupsRepository`, `GroupConfiguration`, and migrations are deleted
- [ ] All command tests pass with updated mocks
- [ ] Flush and append assertions are present in updated tests
- [ ] Read-side MySQL remains unchanged
- [ ] Build is green; all tests pass

---

## Open Questions to Resolve During Implementation

1. `Group.Name` and `Group.Capacity` are `{ get; }` — they need `private set` or `init` for replay. Confirm the preferred approach.
2. Should `Team.Restore` live on `Team` itself (`internal static`), or should the restore logic live inside `Group.Apply(TeamAdded)`?
3. Should `CommandUnitOfWorkBehavior` and `IUnitOfWork` be deleted entirely, or kept for potential future use?
4. Should concurrency conflicts from EventStoreDB (`WrongExpectedVersionException`) be caught in the behavior or in the repository and translated to a domain `Result.Failure`?

---

## On Completion

When this phase is done, update:

- `docs/event-sourcing/PLAN.md`
- `AI.md`
- `.github/copilot-instructions.md`

Mark Phase 2 as complete and move the current phase to Phase 3.
