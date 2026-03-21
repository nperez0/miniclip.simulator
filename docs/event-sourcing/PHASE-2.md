# Phase 2 - EventStoreDB: Write Side Migration

> **Status:** ✅ Complete
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
| `Group` | Private parameterised constructor `Group(Guid id, string name, int capacity)`; `Create`, `AddTeam`, `AddMatch`, `SimulateMatch`; only `SimulateMatch` calls `Enqueue(new MatchPlayed(...))`. Holds `List<Team>` — full `Team` aggregate entity references inside the `Group` aggregate boundary. |
| `Match` | Private parameterless constructor already present; `Create` factory; `SimulateResult` with validation; owned by `Group`. Holds `Team HomeTeam` and `Team AwayTeam` as full entity references. |
| `MatchPlayed` | Rich `record MatchPlayed(...) : IDomainEvent` carrying all denormalised match data (GroupId, GroupName, MatchId, HomeTeamId, HomeTeamName, HomeTeamStrength, HomeScore, AwayTeamId, AwayTeamName, AwayTeamStrength, AwayScore, Round). Only domain event in the system. |
| `GenerateGroupCommandHandler` | Calls `Group.Create`, adds teams, generates fixtures via `fixtureSchedulerService`, calls `.Tap(groupsRepository.Add)`. |
| `SimulateGroupCommandHandler` | Calls `repository.FindAsync(command.GroupId)`, then `groupSimulator.SimulateAllMatches(group)`. Does **not** explicitly save. |
| `GroupsRepository` | EF Core; `FindAsync` uses `.Include(Matches).ThenInclude(HomeTeam / AwayTeam)` to reconstruct `Group` in memory. |
| `SimulatorUnitOfWork` | EF Core; `SaveChangesAsync`, `BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`, `GetTrackedAggregates()`. |
| `CommandUnitOfWorkBehavior` | Wraps every command in `BeginTransaction -> next -> SaveChanges + Commit or Rollback`. |
| `DatabaseConfiguration` | Registers `IRepository<Group>` -> `GroupsRepository` and `IUnitOfWork` -> `SimulatorUnitOfWork`, both backed by `SimulatorWriteDbContext`. Also registers `IRepository<Team>` against the same write context. |
| `MediatorConfiguration` | Pipeline order: `CommandUnitOfWorkBehavior` -> `ReadModelUnitOfWorkBehavior` -> `DomainEventPublisherBehavior`. |
| `IEventStore<T>` | Already implemented: `AppendAsync` (appends uncommitted events, updates `Version`), `LoadAsync` (replays stream, returns null when stream not found). The public interface will change in this phase — `AppendAsync` moves off the public contract. |

---

## Critical Design Gap

The current aggregate only emits events in `SimulateMatch`. `Group.Create`, `AddTeam`, and `AddMatch` are **silent** — they mutate state without raising any events.

This makes event-sourced replay impossible for the write side: `EventStoreDbEventStore.LoadAsync` replays a stream to reconstruct aggregate state, but after a `GenerateGroup` command the stream would be **empty** (no events were appended), and `AppendAsync` exits early when `DequeueUncommittedEvents()` returns an empty array.

Additionally, `SimulateGroupCommandHandler` relies on `Group` having fully populated `Match` objects with `HomeTeam` and `AwayTeam` entity references (including `Strength`) to run the simulation. A `Group` replayed only from `MatchPlayed` events would not have those references for **unplayed** matches.

Finally, `Group` and `Match` currently hold full `Team` **aggregate** entity references in their internal state. This violates the DDD aggregate boundary rule — an aggregate must not hold object references to another aggregate; it should only hold an identifier or a value object snapshot of the data it needs. The `Group` aggregate needs team data (Id, Name, Strength) to replay state and to simulate matches, but it does not need the full `Team` lifecycle.

**These three gaps must be closed in Phase 2 before any infrastructure change makes sense.**

---

## Proposed Design

### 1. Introduce `TeamInfo` as a value object snapshot inside `Group`

`Group` and `Match` currently hold full `Team` entity references. The fix is to capture only the data the aggregate needs at the moment a team is added, and store it as a value object snapshot:

```csharp
public record TeamInfo(Guid Id, string Name, int Strength);
```

`Group` stores `List<TeamInfo>` internally. `Match` holds `TeamInfo HomeTeam` and `TeamInfo AwayTeam`. `GroupSimulator` is unchanged — it still accesses `match.HomeTeam.Strength`, which works because `TeamInfo` has a `Strength` property.

The `AddTeam` signature changes from `AddTeam(Team team)` to `AddTeam(TeamInfo teamInfo)`. In `GenerateGroupCommandHandler`, the `Team` aggregate fetched from `IRepository<Team>` is immediately converted:

```csharp
group.AddTeam(new TeamInfo(team.Id, team.Name, team.Strength))
```

This explicitly documents the "snapshot at creation time" design intent at the call site. The handler is the only place that knows about both `Team` (the aggregate) and `TeamInfo` (the snapshot).

After group creation, the `Group` aggregate is fully self-contained — it never needs to fetch team data again, including during simulation replay.

### 2. Add three new domain events to close the replay gap

| New Event | Emitted From | Purpose |
|---|---|---|
| `GroupCreated(GroupId, Name, Capacity)` | `Group.Create` | Records group identity and configuration. |
| `TeamAdded(GroupId, TeamId, Name, Strength)` | `Group.AddTeam` | Records which team was added, with enough data to reconstruct the `Team` entity for simulation. |
| `MatchScheduled(GroupId, MatchId, HomeTeamId, AwayTeamId, Round)` | `Group.AddMatch` | Records which fixture was scheduled. Team data is already known from `TeamAdded` events. |

Together with the existing `MatchPlayed`, these four events capture the full lifecycle of a `Group` and make complete replay possible.

### 3. Add a private parameterless constructor and `Apply` overrides to `Group`

`EventStoreDbEventStore.LoadAsync` uses `Activator.CreateInstance(typeof(T), nonPublic: true)` to create the shell aggregate, then calls `ReplayEvent(event, version)` for each stored event. `Group` needs:

- `private Group()` — initialises `teams` and `matches` lists (parameterless, for replay only)
- `protected override void Apply(IDomainEvent @event)` — dispatches to a private handler per event type
- `private void Apply(GroupCreated e)` — sets `Id`, `Name`, `Capacity`
- `private void Apply(TeamAdded e)` — adds a `TeamInfo` snapshot to the internal list
- `private void Apply(MatchScheduled e)` — looks up the two `TeamInfo` snapshots already in the list and reconstructs a `Match`
- `private void Apply(MatchPlayed e)` — finds the match by Id and calls `match.ApplyResult(e.HomeScore, e.AwayScore)`

### 4. Add `Match.ApplyResult` for replay

`Match.SimulateResult` validates inputs (no negative scores, not already played) before mutating state. During replay, the event is already valid and must be applied without failure. Add:

`internal void ApplyResult(int homeScore, int awayScore)` — sets `HomeScore`, `AwayScore`, `IsPlayed = true` with no validation.

### 5. Keep `IRepository<Group>` — back it with a generic event-sourced implementation

Command handlers already depend on `IRepository<Group>` via constructor injection. Keeping the interface avoids touching the handlers.

The implementation is a generic `EventSourcedRepository<T>` that lives in `Miniclip.Core.EventSourcing`. It has no aggregate-specific logic — all replay and event-emission behaviour lives in the aggregate and the store. Any future event-sourced aggregate gets its repository for free just by registering the type in DI.

`IRepository<T>.Add` is synchronous (called via `.Tap(...)` in the handler). It delegates directly to `eventStore.Track(aggregate)`, which registers the aggregate with the session internally.

`IRepository<T>.FindAsync` calls `eventStore.LoadAsync`, which automatically tracks the loaded aggregate inside the store. The repository needs no extra tracking call.

### 6. Introduce `IEventStoreSession` and `EventStoreCommandBehavior`

`IEventStoreSession` is a scoped service that collects deferred append actions and executes them all on `CommitAsync`:

```csharp
// Miniclip.Core.EventSourcing
public interface IEventStoreSession
{
    void Track(Func<CancellationToken, Task> commitAction);
    Task CommitAsync(CancellationToken cancellationToken = default);
}
```

`IEventStore<T>` is updated: `AppendAsync` is removed from the public contract and replaced with `Track(T aggregate)`. `LoadAsync` auto-tracks the loaded aggregate internally by calling `session.Track(ct => AppendAsync(aggregate, ct))` before returning. `Track(T aggregate)` does the same for new aggregates.

```csharp
// Updated IEventStore<T>
public interface IEventStore<T> where T : AggregateRoot
{
    void Track(T aggregate);
    Task<T?> LoadAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}
```

`EventStoreCommandBehavior<TRequest, TResponse>` replaces `CommandUnitOfWorkBehavior` in the mediator pipeline. It injects a single `IEventStoreSession` and:

- skips non-command requests (using the existing `IsCommand()` extension)
- calls `next(request, cancellationToken)`
- if the response `IsSuccessful()`, calls `session.CommitAsync(cancellationToken)`
- on exception or failure, does nothing (no appends were made)

### 7. Handle the `Team` repository after write context removal

`IRepository<Team>` is used only in `GenerateGroupCommandHandler` — to fetch team data and convert it to `TeamInfo` snapshots. It is **not** needed during simulation; the `Group` aggregate carries all the team data it needs. This means `IRepository<Team>` remains a read-only reference-data lookup used exclusively at group creation time.

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
- `TeamInfo` value object in `Miniclip.Simulator.Domain`
- `Group`: stores `List<TeamInfo>` internally; `AddTeam` accepts `TeamInfo`; private parameterless constructor, `Apply` overrides for all four events
- `Match`: holds `TeamInfo HomeTeam / AwayTeam` instead of `Team` entity references; `Restore` factory accepts `TeamInfo`; `ApplyResult` for replay without validation
- `IFixtureSchedulerService` / `RoundRobinScheduler`: updated to work with `TeamInfo` (since `group.Teams` now returns `IReadOnlyCollection<TeamInfo>`)
- `IEventStoreSession` interface and `EventStoreSession` implementation
- `IEventStore<T>` updated: `AppendAsync` removed from public contract, `Track(T aggregate)` added, `LoadAsync` auto-tracks
- `EventSourcedRepository<T>` (generic, in `Miniclip.Core.EventSourcing`, implements `IRepository<T>`)
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
| `Miniclip.Simulator.Domain` | Add `TeamInfo` value object. Add `GroupCreated`, `TeamAdded`, `MatchScheduled` events; emit them from `Group.Create`, `AddTeam`, `AddMatch`. Change `Group` to store `List<TeamInfo>`; change `AddTeam` to accept `TeamInfo`. Change `Match` to hold `TeamInfo` instead of `Team` references; add `Match.Restore(TeamInfo, TeamInfo)` and `Match.ApplyResult`. Add private parameterless constructor and `Apply` overrides to `Group`. Update `IFixtureSchedulerService` and `RoundRobinScheduler` for `TeamInfo`. |
| `Miniclip.Core.EventSourcing` | Update `IEventStore<T>`: remove `AppendAsync`, add `Track(T aggregate)`. Add `IEventStoreSession` interface. Add generic `EventSourcedRepository<T>`. |
| `Miniclip.Core.EventSourcing.EventStoreDB` | Update `EventStoreDbEventStore<T>`: inject `IEventStoreSession`; `LoadAsync` auto-tracks; `Track` registers deferred append; `AppendAsync` becomes private. Add `EventStoreSession` implementation. |
| `Miniclip.Core.Application` | Add `EventStoreCommandBehavior<TRequest, TResponse>`; remove or keep `CommandUnitOfWorkBehavior` (becomes dead code). |
| `Miniclip.Simulator.Infrastructure.Write` | Delete EF `GroupsRepository`, `SimulatorUnitOfWork`, `SimulatorWriteDbContext`, `GroupConfiguration`, and migrations. No new repository file needed — the generic one from `Core.EventSourcing` covers it. |
| `Miniclip.Simulator.Api` | Update `DatabaseConfiguration` and `MediatorConfiguration` for event-sourced wiring. |
| `Miniclip.Simulator.Application.Commands.UnitTests` | Update mocks and assertions for all generation and simulation tests. |

---

## Implementation Plan

### Step 1 — Add `TeamInfo` and update `Group` and `Match` internal structure

Create `TeamInfo` in `Miniclip.Simulator.Domain/Aggregates/Groups/`:

```csharp
public record TeamInfo(Guid Id, string Name, int Strength);
```

Change `Group` internal state:

- Replace `List<Team> teams` with `List<TeamInfo> teams`
- Change `AddTeam(Team team)` to `AddTeam(TeamInfo teamInfo)` — stores the snapshot directly
- Change `AddMatch` to accept `TeamInfo homeTeam, TeamInfo awayTeam` instead of `Team` references
- Change `Teams` property to return `IReadOnlyCollection<TeamInfo>`

Change `Match` internal state:

- Replace `Team HomeTeam` and `Team AwayTeam` with `TeamInfo HomeTeam` and `TeamInfo AwayTeam`
- Remove `HomeTeamId` and `AwayTeamId` backing fields (they are now on `TeamInfo`)
- Add `internal static Match Restore(Guid id, TeamInfo home, TeamInfo away, int round)` — creates a `Match` without the same-team guard
- Update `Match.Create` to accept `TeamInfo` instead of `Team`

Update `GenerateGroupCommandHandler` — convert `Team` aggregates to `TeamInfo` before calling `AddTeam`:

```csharp
private static Result<Group> AddTeams(Group group, IEnumerable<Team> teams)
    => teams.Traverse(t => group.AddTeam(new TeamInfo(t.Id, t.Name, t.Strength)))
        .Map(() => group);
```

Update `IFixtureSchedulerService` / `RoundRobinScheduler` — `group.Teams` now returns `IReadOnlyCollection<TeamInfo>`; update any usage of `team.Id` or `team.Name` accordingly (no behavioural change, only type change).

### Step 2 — Add new domain events

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
- `Group.AddTeam`: call `Enqueue(new TeamAdded(Id, teamInfo.Id, teamInfo.Name, teamInfo.Strength))` before returning `Result.Success()`
- `Group.AddMatch`: call `Enqueue(new MatchScheduled(Id, id, homeTeam.Id, awayTeam.Id, round))` after the match is added

> Note: `Enqueue` is called **after** state mutation succeeds so that events are only raised for valid transitions.

### Step 3 — Make `Group` replayable

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
private void Apply(TeamAdded e)    { teams.Add(new TeamInfo(e.TeamId, e.Name, e.Strength)); }
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

`Match.Restore` is an internal static factory that creates a `Match` from `TeamInfo` snapshots without running the same-team validation guard:

- `Match.Restore(Guid id, TeamInfo home, TeamInfo away, int round)` — creates a `Match` directly from `TeamInfo` values

### Step 4 — Add `Match.ApplyResult`

```csharp
internal void ApplyResult(int homeScore, int awayScore)
{
    HomeScore = homeScore;
    AwayScore = awayScore;
    IsPlayed = true;
}
```

### Step 5 — Add `IEventStoreSession` and update `IEventStore<T>`

In `Miniclip.Core.EventSourcing`, add:

```csharp
public interface IEventStoreSession
{
    void Track(Func<CancellationToken, Task> commitAction);
    Task CommitAsync(CancellationToken cancellationToken = default);
}
```

Update `IEventStore<T>` — remove `AppendAsync` from the public contract and add `Track`:

```csharp
public interface IEventStore<T> where T : AggregateRoot
{
    void Track(T aggregate);
    Task<T?> LoadAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}
```

In `Miniclip.Core.EventSourcing.EventStoreDB`, add `EventStoreSession`:

```csharp
public sealed class EventStoreSession : IEventStoreSession
{
    private readonly List<Func<CancellationToken, Task>> _pending = [];

    public void Track(Func<CancellationToken, Task> commitAction)
        => _pending.Add(commitAction);

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        foreach (var commit in _pending)
            await commit(cancellationToken);
    }
}
```

Update `EventStoreDbEventStore<T>` to inject `IEventStoreSession`. `AppendAsync` becomes private. `Track` and `LoadAsync` register deferred appends with the session:

```csharp
public sealed class EventStoreDbEventStore<T>(
    EventStoreClient client,
    IEventSerializer serializer,
    IEventStoreSession session) : IEventStore<T>
    where T : AggregateRoot
{
    public void Track(T aggregate)
        => session.Track(ct => AppendAsync(aggregate, ct));

    public async Task<T?> LoadAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var aggregate = await LoadInternalAsync(aggregateId, cancellationToken);
        if (aggregate is not null)
            Track(aggregate);
        return aggregate;
    }

    private Task AppendAsync(T aggregate, CancellationToken cancellationToken) { /* existing logic */ }
}
```

Register `IEventStoreSession` as scoped in `ServiceCollectionExtensions`:

```csharp
services.AddScoped<IEventStoreSession, EventStoreSession>();
```

### Step 6 — Add generic `EventSourcedRepository<T>`

Create in `Miniclip.Core.EventSourcing/`. The class has no aggregate-specific logic — it is a pure adapter between `IRepository<T>` and `IEventStore<T>`:

```csharp
public class EventSourcedRepository<T>(IEventStore<T> eventStore) : IRepository<T>
    where T : AggregateRoot
{
    public void Add(T aggregate) => eventStore.Track(aggregate);

    public Task<T?> FindAsync(Guid id, CancellationToken cancellationToken)
        => eventStore.LoadAsync(id, cancellationToken);

    public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException("GetAllAsync is not supported for event-sourced repositories.");

    public void Delete(T aggregate)
        => throw new NotSupportedException("Delete is not supported for event-sourced repositories.");
}
```

Any future event-sourced aggregate gets its repository for free just by registering `IRepository<ConcreteType>` against this class in DI.

### Step 7 — Add `EventStoreCommandBehavior`

Create in `Miniclip.Core.Application/Behaviors/`. The behavior has a single dependency on `IEventStoreSession`:

```csharp
public class EventStoreCommandBehavior<TRequest, TResponse>(IEventStoreSession session)
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
            await session.CommitAsync(cancellationToken);

        return response;
    }
}
```

### Step 8 — Update DI wiring

In `DatabaseConfiguration`:

- Remove `SimulatorWriteDbContext` registration
- Remove `IRepository<Group>` -> `GroupsRepository`
- Remove `IUnitOfWork` -> `SimulatorUnitOfWork`
- Register `IRepository<Group>` against `EventSourcedRepository<T>`:

```csharp
services.AddScoped<IRepository<Group>, EventSourcedRepository<Group>>();
```

- `IEventStore<Group>` and `IEventStoreSession` are already registered by `AddEventStoreDbClient` (update that extension to include `IEventStoreSession`)
- Re-register `IRepository<Team>` against `SimulatorReadDbContext` (teams are read-only reference data)
- Call `AddEventStoreDbClient(connectionString)` with the EventStoreDB connection string from configuration

In `MediatorConfiguration`:

- Replace `CommandUnitOfWorkBehavior<,>` with `EventStoreCommandBehavior<,>`

### Step 9 — Remove EF write infrastructure

Delete from `Miniclip.Simulator.Infrastructure.Write`:

- `Persistence/SimulatorWriteDbContext.cs`
- `Persistence/SimulatorUnitOfWork.cs`
- `Persistence/Repositories/GroupsRepository.cs`
- `Persistence/Configurations/GroupConfiguration.cs`
- `Migrations/` folder (all migration files)

Verify that `TeamConfiguration.cs` can also be removed or moved. If `IRepository<Team>` is re-registered against the read context, `TeamConfiguration` in the write project becomes dead code.

Update `DatabaseConfiguration.InitializeDatabases` to remove the write context migration call.

### Step 10 — Update command tests

**`WhenGeneratingGroups`:**

- `GroupRepository.Add` assertion remains (the call is unchanged in the handler)
- Since `Group.Create`, `AddTeam`, and `AddMatch` now enqueue events, domain unit tests may need updating
- Mock `IEventStoreSession` and assert `CommitAsync` is called once after a successful command

**`WhenSimulatingGroups`:**

- `GroupRepository.FindAsync` mock still returns a `Group` built via `GroupMother` — valid because the test constructs the group in-memory
- Mock `IEventStoreSession` and assert `CommitAsync` is called once after simulation
- Add a test for `FindAsync` returning `null` (stream does not exist in EventStoreDB)

**New test: concurrency conflict:**

- Mock `IEventStoreSession.CommitAsync` to throw `WrongExpectedVersionException`
- Assert the command returns a failure result with a conflict message

---

## Definition of Done

- [x] `GroupCreated`, `TeamAdded`, and `MatchScheduled` events exist and are emitted from the aggregate
- [x] `TeamInfo` value object is defined in `Miniclip.Simulator.Domain`
- [x] `Group` stores `List<TeamInfo>` internally; `AddTeam` accepts `TeamInfo`
- [x] `Match` holds `TeamInfo` instead of `Team` entity references
- [x] `Group` has a private parameterless constructor and `Apply` overrides for all four event types
- [x] `Match` has `ApplyResult` for replay
- [x] `IEventStoreSession` is defined in `Miniclip.Core.EventSourcing` and `EventStoreSession` is implemented in `Miniclip.Core.EventSourcing.EventStoreDB`
- [x] `IEventStore<T>` updated: `AppendAsync` removed from the public contract, `Track` added, `LoadAsync` auto-tracks
- [x] `EventSourcedRepository<T>` is in `Miniclip.Core.EventSourcing` and is the registered `IRepository<Group>` in production
- [x] `EventStoreCommandBehavior` is in the mediator pipeline instead of `CommandUnitOfWorkBehavior`
- [x] `GenerateGroupCommand` appends events to EventStoreDB
- [x] `SimulateGroupCommand` loads the group from EventStoreDB and appends new `MatchPlayed` events
- [x] `SimulatorUnitOfWork`, EF `GroupsRepository`, `GroupConfiguration`, and `CommandUnitOfWorkBehavior` deleted
- [x] All command tests pass with updated mocks
- [x] Read-side MySQL remains unchanged
- [x] Build is green; all tests pass (161 of 161)

---

## Open Questions — Resolved

1. **`Group.Name` and `Group.Capacity` init** — Changed to `{ get; private set; }` to allow replay assignment.
2. **`TeamInfo` location** — Placed in `Miniclip.Simulator.Domain/Aggregates/Groups/ValueObjects/` (domain-specific, not shared kernel).
3. **`CommandUnitOfWorkBehavior` and `IUnitOfWork`** — Both deleted. `IUnitOfWork` had a single remaining consumer (`DomainEventPublisherBehavior`) which was updated to use `IEventStoreSession.GetCommittedEvents()` instead.
4. **Concurrency conflicts** — Deferred to Phase 3/5. `WrongExpectedVersionException` from EventStoreDB propagates as an unhandled exception for now.
5. **`SimulatorWriteDbContext`** — Kept (not deleted). It still serves `Team` reference data via `TeamConfiguration`. Only `GroupConfiguration` was removed.
6. **`IEventStoreSession.Track` signature** — Changed to `Func<CancellationToken, Task<IDomainEvent[]>>` so the session can collect committed events and expose them to `DomainEventPublisherBehavior` via `GetCommittedEvents()`. This solves the pipeline ordering problem where events would otherwise be drained before being appended.

---

## On Completion

When this phase is done, update:

- `docs/event-sourcing/PLAN.md`
- `AI.md`
- `.github/copilot-instructions.md`

Mark Phase 2 as complete and move the current phase to Phase 3.
