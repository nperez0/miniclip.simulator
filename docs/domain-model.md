# Domain Model

## Aggregates & Entities

### `Group` (Aggregate Root)

The central aggregate of the simulator. Owns the full lifecycle of a football group stage.

| Property | Type | Rule |
|---|---|---|
| `Id` | `Guid` | Assigned at creation |
| `Name` | `string` | Must not be null or whitespace |
| `Capacity` | `int` | 2–6 teams |
| `Teams` | `IReadOnlyCollection<TeamInfo>` | Cannot exceed `Capacity`; no duplicate IDs |
| `Matches` | `IReadOnlyCollection<Match>` | Populated by fixture scheduling; owned by `Group` |

**Business rules enforced by `Group`:**
- `Create(id, name, capacity)` — fails if name is empty or capacity is out of range.
- `AddTeam(team)` — fails if already at capacity, or if the team already belongs to the group.
- `AddMatch(id, homeTeam, awayTeam, round)` — delegates to `Match.Create`; fails if home == away.
- `SimulateMatch(matchId, homeScore, awayScore)` — fails if match not found or already played; fires `MatchPlayed`.

---

### `Team` (Aggregate Root)

Represents a football team. Stored as an event stream in EventStoreDB (`team-{id}`). A fixed squad of 10 teams is seeded at startup by `TeamDataSeeder`.

| Property | Type | Rule |
|---|---|---|
| `Id` | `Guid` | Assigned at creation |
| `Name` | `string` | Must not be null or whitespace |
| `Strength` | `int` | 0–100; directly influences Poisson expected goals |

Emits `TeamRegistered` on creation. `Team` instances are converted to `TeamInfo` snapshots when added to a `Group` to respect aggregate boundaries.

---

### `Match` (Entity, owned by `Group`)

Represents a single fixture inside a group.

| Property | Type | Rule |
|---|---|---|
| `Id` | `Guid` | Assigned at creation |
| `HomeTeam` / `AwayTeam` | `TeamInfo` | Must be different teams |
| `HomeScore` / `AwayScore` | `int` | Set only during simulation; must be ≥ 0 |
| `Round` | `int` | The round in which this fixture is played |
| `IsPlayed` | `bool` | Once `true`, the match cannot be simulated again |

---

## Domain Events

All domain events implement `IDomainEvent` and carry an `AggregateId`.

| Event | Aggregate | Stream | Published to Kafka |
|---|---|---|---|
| `TeamRegistered` | `Team` | `team-{id}` | No (seeder only) |
| `GroupCreated` | `Group` | `group-{id}` | No |
| `TeamAdded` | `Group` | `group-{id}` | No |
| `MatchScheduled` | `Group` | `group-{id}` | No |
| `MatchPlayed` | `Group` | `group-{id}` | **Yes** — drives `GroupStandingsProjection` + `MatchResultProjection` |

Events are **enqueued** inside the aggregate via `AggregateRoot.Enqueue` during `Create`/command processing. `Apply(IDomainEvent)` is called only during stream **replay** from EventStoreDB — it is not invoked during normal command processing.

After a command completes, `EventStoreCommandBehavior` commits the session (appends events to EventStoreDB), then `DomainEventPublisherBehavior` publishes committed events to Kafka.

---

## Domain Services

### Fixture Scheduling

**`IFixtureSchedulerService`** → **`FixtureSchedulerService`**

Orchestrates scheduling for a `Group`. Uses a factory (`IFixtureSchedulerFactory`) to resolve the correct scheduler implementation.

**`RoundRobinScheduler`** — core algorithm:

1. If team count is **odd**, inserts `TeamInfo.Dummy` (a static `(Guid.Empty, "Dummy", 0)` value object) to make it even.
2. Total rounds = `capacity - 1` (even) or `capacity` (odd).
3. In each round, pairs teams by index from both ends of a rotated list.
4. Matches involving `TeamInfo.Dummy` are **skipped** (bye round for that real team).
5. Home/away assignment is balanced using per-team counters (`homeCounter`, `awayCounter`).

> **Example:** 4 teams → 3 rounds × 2 matches = 6 total matches.  
> **Example:** 5 teams → 5 rounds, dummy added → 2 matches per round, but 1 skipped per round = 10 total matches.

---

### Match Simulation

**`IMatchSimulator`** → **`MatchSimulator`**

Produces a score for a single match using a **Poisson distribution**.

```
adjustedHomeStrength = homeTeamStrength × 1.1   ← home advantage
expectedGoals        = (strength / 100) × 3.5   ← base expected goals
score                = sampled from Poisson(expectedGoals)
```

- A team with `Strength = 100` has `expectedGoals ≈ 3.5`.
- A team with `Strength = 0` has `expectedGoals = 0` (always scores 0).
- Scores are capped at `MaxGoals = 10`.

**`IGroupSimulator`** → **`GroupSimulator`**

Iterates over all unplayed matches in a group, calls `IMatchSimulator.SimulateMatch`, and applies the result via `Group.SimulateMatch`.

---

## Read Models (Projections output)

### `GroupStandingsModel`

Materialised view per team per group. Updated after every `MatchPlayed` event.

| Field | Description |
|---|---|
| `Position` | Ranking within the group (1 = best) |
| `Points` | 3 for win, 1 for draw, 0 for loss |
| `Wins` / `Draws` / `Losses` | Match outcome counts |
| `GoalsFor` / `GoalsAgainst` | Cumulative goals |
| `GoalDifference` | `GoalsFor - GoalsAgainst` |
| `MatchesPlayed` | Total matches simulated |
| `QualifiesForKnockout` | Indicates top-2 finisher |
| `TeamStrength` | Stored for display purposes |
| `LastUpdated` | Timestamp of last projection update |

**Position ordering:** Points → Goal Difference → Goals For. Recalculated from scratch after each `MatchPlayed` by `RecalculatePositionService`.

---

### `MatchResultModel`

Immutable record of a played match. Created once per `MatchPlayed` event.

| Field | Description |
|---|---|
| `MatchId` | Links to the write-side `Match` |
| `Round` | Round number |
| `HomeTeamName` / `AwayTeamName` | Denormalised for fast reads |
| `HomeScore` / `AwayScore` | Final score |
| `PlayedAt` | UTC timestamp of simulation |

---

## Invariants Summary

| Rule | Where enforced |
|---|---|
| Group capacity: 2–6 | `Group.Create` |
| Group name must not be empty | `Group.Create` |
| No duplicate teams in group | `Group.AddTeam` |
| Home ≠ Away in a match | `Match.Create` |
| Match can only be simulated once | `Match.SimulateResult` |
| Scores must be ≥ 0 | `Match.SimulateResult` |
| Team strength: 0–100 | `Team.Create` |
| Team name must not be empty | `Team.Create` |
