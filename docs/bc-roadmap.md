# Bounded Context Roadmap

This document captures the full domain design for the Miniclip Simulator platform across all Bounded Contexts.
Each BC section describes its ubiquitous language, aggregates, business rules, domain events, and how it integrates
with other BCs. Implementation details (infrastructure, Kafka config, EF mappings) are out of scope here.

---

## Context Map

```
┌─────────────────────────────────────────────────────────────────────┐
│                         TEAMS BC                                    │
│  Owns team identity, strength ratings, and team lifecycle.          │
│  Upstream of everything — no BC dependencies.                       │
└──────────────────────────────┬──────────────────────────────────────┘
                               │ TeamRegistered, TeamStrengthUpdated
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       SIMULATOR BC                                  │
│  Owns group creation, fixture scheduling, match simulation,         │
│  round and group progression. Consumes team data via ACL.           │
└──────────────┬─────────────────────────┬────────────────────────────┘
               │ RoundCompleted          │ GroupCompleted
               ▼                         ▼
┌──────────────────────┐     ┌───────────────────────────────────────┐
│     BETTING BC       │     │           NOTIFICATIONS BC            │
│  Owns bet placement  │     │  Pure reactor. No aggregates.         │
│  and settlement.     │     │  Sends alerts on match results,       │
│  Manages betting     │     │  bet outcomes, and window events.     │
│  windows per round.  │     └───────────────────────────────────────┘
└──────────┬───────────┘
           │ BetPlaced (saga)
           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        WALLET BC                                    │
│  Owns user balances. Participates in the PlaceBet saga as the       │
│  balance reservation and credit/debit authority.                    │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Phase 1 — Extract Teams BC

> **Goal:** make `Team` a first-class BC with its own service and lifecycle,
> freeing the Simulator BC from owning team master data.

### Why this is a genuine BC split

In the current codebase, the Simulator BC owns both team master data (`Team` aggregate) and group simulation
(`Group` aggregate). These speak different languages:

| In Simulator BC today | In Teams BC (after split) |
|---|---|
| "Team" = a snapshot of name + strength frozen at group creation | "Team" = a managed entity: registered, updated, deactivated |
| `TeamInfo` value object — immutable once captured | `Team` aggregate — mutable, has a history |

The `TeamInfo.FromTeam(team)` factory method is the seam. After the split it is replaced by constructing
`TeamInfo` from a local read model that the Simulator BC maintains.

### Teams BC — Ubiquitous Language

| Term | Meaning |
|---|---|
| **Team** | A football club with a name and a strength rating (0–100). |
| **Strength** | A numeric value influencing match outcome probability. Managed here; consumed by Simulator. |
| **Registration** | The act of enrolling a team into the platform for the first time. |
| **Deactivation** | Marking a team as no longer available for new groups. Does not affect existing groups. |

### Teams BC — Aggregates

#### `Team`

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Permanent identity |
| `Name` | `string` | Unique within the platform |
| `Strength` | `int` | 0–100 |
| `IsActive` | `bool` | Deactivated teams cannot be added to new groups |

**Business rules:**
- A team name must be unique across the platform.
- Strength must be between 0 and 100.
- A deactivated team cannot have its strength updated.
- A team cannot be deleted — only deactivated.

**Domain events:**

| Event | Raised when |
|---|---|
| `TeamRegistered` | A new team is enrolled. |
| `StrengthUpdated` | A team's strength rating is changed. |
| `TeamDeactivated` | A team is marked inactive. |

**Integration events published (cross-BC):**

| Integration event | Triggered by | Consumed by |
|---|---|---|
| `TeamRegisteredIntegrationEvent` | `TeamRegistered` | Simulator BC (updates local team read model) |
| `TeamStrengthUpdatedIntegrationEvent` | `StrengthUpdated` | Simulator BC (updates local team read model — affects future groups only) |

### Impact on Simulator BC

- The `Team` aggregate and `TeamRegistered` domain event are removed from the Simulator BC.
- The Simulator BC gains a **local team read model** (an ACL): a table of `(TeamId, Name, Strength, IsActive)`
  populated by consuming `TeamRegisteredIntegrationEvent` and `TeamStrengthUpdatedIntegrationEvent`.
- Group creation queries this local read model — no synchronous call to the Teams BC.
- `TeamInfo` value object remains. It is now constructed from the local read model instead of from the `Team` aggregate directly.
- **Snapshot rule:** strength is captured at group creation time. A subsequent `StrengthUpdated` event does not
  affect groups already in progress. This is intentional — simulation fairness requires stable team data per group.

---

## Phase 2 — Evolve Simulator BC: Rounds and Tournament

> **Goal:** introduce `RoundCompleted` and `GroupCompleted` as first-class events,
> and add a `Tournament` aggregate to coordinate large-scale simulation runs.

### Simulator BC — Ubiquitous Language (updated)

| Term | Meaning |
|---|---|
| **Group** | A set of 2–6 teams that play a full round-robin. The core simulation unit. |
| **Round** | A set of matches where every team plays exactly once. Derived from the round-robin schedule. |
| **Stage** | A collection of groups that are simulated together within a time window. Owned by `Tournament`. |
| **Tournament** | A long-running entity that creates groups in batches, tracks their progress, and declares overall completion. |
| **Fixture** | A scheduled match within a group: home team, away team, round number. |
| **Standing** | The ordered position of a team within a group after some or all matches are played. |

### New domain events for `Group`

| Event | Raised when |
|---|---|
| `RoundCompleted` | All matches in a round have been simulated. Carries round number and current standings snapshot. |
| `GroupCompleted` | All rounds in the group are done. Carries final standings. |

**Business rules for rounds:**
- A round is complete when every non-bye match in that round has been played.
- `RoundCompleted` is raised by the `Group` aggregate after the last match of a round is simulated — not by an external orchestrator.
- `GroupCompleted` is raised automatically after the final round is complete.
- A group that has been completed cannot accept further simulation commands.

### `Tournament` aggregate

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `Name` | `string` | e.g. "Monday Evening Stage" |
| `TeamCount` | `int` | How many teams per group (2–6) |
| `GroupCount` | `int` | Total groups to create |
| `GroupsCompleted` | `int` | Increments as `GroupCompleted` events are received |
| `Status` | `enum` | `Pending → Running → Completed` |

**Business rules:**
- A tournament can only be started once all target groups have been created.
- A tournament moves to `Completed` when `GroupsCompleted == GroupCount`.
- A tournament cannot be cancelled after it has started.
- Group creation within a tournament is rate-limited to avoid write storms (batch size is a configuration concern, not a domain rule).

**Domain events:**

| Event | Raised when |
|---|---|
| `TournamentCreated` | A new tournament is registered with a team count and group count. |
| `TournamentStarted` | All groups have been created and at least one simulation has begun. |
| `GroupRegistered` | A group is associated with this tournament. |
| `TournamentCompleted` | All groups within the tournament have completed. |

**Integration events published (cross-BC):**

| Integration event | Triggered by | Consumed by |
|---|---|---|
| `RoundCompletedIntegrationEvent` | `RoundCompleted` | Betting BC (close betting window, settle round bets) |
| `GroupCompletedIntegrationEvent` | `GroupCompleted` | Betting BC (settle final standings bets), Notifications BC |
| `TournamentCompletedIntegrationEvent` | `TournamentCompleted` | Notifications BC |

---

## Phase 3 — Betting BC

> **Goal:** allow users to place bets on round outcomes or final standings,
> with time-bounded betting windows and a choreography-based settlement saga.

### Betting BC — Ubiquitous Language

| Term | Meaning |
|---|---|
| **Bet** | A user's prediction about a group outcome (round result or final standings), backed by a wager amount. |
| **Betting Window** | A time-bounded period during which bets on a specific group and round (or final standings) are accepted. |
| **Wager** | The amount of balance a user commits to a bet. Reserved from the Wallet BC at placement time. |
| **Settlement** | The act of determining whether a bet won or lost and triggering credit or release to the Wallet BC. |
| **Prediction** | What the user is betting on: a specific team finishing in a specific position, or a round result. |

### Aggregates

#### `BettingWindow`

Controls when bets can be placed for a given group and round.

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `GroupId` | `Guid` | The group this window is for |
| `Round` | `int?` | Null means "final standings" |
| `Status` | `enum` | `Open → Closed → Settled` |

**Business rules:**
- A window is opened by the reaction to `GroupCompletedIntegrationEvent` (for final standings)
  or at tournament creation time for round-specific windows.
- A window can only be closed once — it cannot be reopened.
- A bet can only be placed while the window is `Open`.
- When a window moves to `Closed`, all open bets for that window become eligible for settlement.
- A window moves to `Settled` when all its bets have been settled.

**Domain events:**

| Event | Raised when |
|---|---|
| `BettingWindowOpened` | A new window is created and accepting bets. |
| `BettingWindowClosed` | The window is closed; no new bets accepted. Triggered by a `RoundCompletedIntegrationEvent`. |
| `BettingWindowSettled` | All bets within this window have been resolved. |

#### `Bet`

Represents a single user wager on a prediction.

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `UserId` | `Guid` | |
| `WindowId` | `Guid` | The betting window this bet belongs to |
| `Prediction` | value object | What the user is betting on |
| `WagerAmount` | `decimal` | Amount reserved from wallet |
| `Status` | `enum` | `Pending → Confirmed → Won → Lost → Cancelled` |

**Business rules:**
- A bet can only be placed against an `Open` window.
- A bet starts in `Pending` status until the Wallet BC confirms the balance reservation.
- If the Wallet BC cannot reserve the balance, the bet is `Cancelled`.
- A `Confirmed` bet is settled when its window is closed and results are available.
- A settled bet cannot be modified or cancelled.
- A user cannot place two bets on the same prediction within the same window.

**Domain events:**

| Event | Raised when |
|---|---|
| `BetInitiated` | User requests to place a bet; wager is not yet reserved. |
| `BetConfirmed` | Wallet BC confirmed the balance reservation. |
| `BetCancelled` | Wallet BC rejected the reservation, or user cancelled before confirmation. |
| `BetWon` | Settlement determined the prediction was correct. |
| `BetLost` | Settlement determined the prediction was wrong. |

### PlaceBet Choreography Saga

```
User: PlaceBetCommand
  → Bet created in "Pending" state
  → BetInitiatedIntegrationEvent published

WALLET BC reacts:
  BalanceReservation.Reserve(userId, amount, betId)
    → success: BalanceReservedIntegrationEvent
    → failure: InsufficientFundsIntegrationEvent

BETTING BC reacts:
  on BalanceReservedIntegrationEvent  → Bet.Confirm()   → BetConfirmedIntegrationEvent
  on InsufficientFundsIntegrationEvent → Bet.Cancel()   → BetCancelledIntegrationEvent

WALLET BC reacts:
  on BetCancelledIntegrationEvent → BalanceReservation.Release()
```

### Settlement Choreography

```
SIMULATOR BC publishes: RoundCompletedIntegrationEvent
BETTING BC reacts:
  BettingWindow.Close(round)
  foreach Bet in window:
    result = evaluate(bet.Prediction, roundStandings)
    result is win  → Bet.Win()  → BetWonIntegrationEvent
    result is loss → Bet.Lose() → BetLostIntegrationEvent

WALLET BC reacts:
  on BetWonIntegrationEvent  → Wallet.Credit(userId, winnings)
  on BetLostIntegrationEvent → BalanceReservation.Consume(reservationId)  // forfeit
```

**Integration events published (cross-BC):**

| Integration event | Triggered by | Consumed by |
|---|---|---|
| `BetInitiatedIntegrationEvent` | `BetInitiated` | Wallet BC (reserve balance) |
| `BetConfirmedIntegrationEvent` | `BetConfirmed` | Notifications BC |
| `BetCancelledIntegrationEvent` | `BetCancelled` | Wallet BC (release reservation), Notifications BC |
| `BetWonIntegrationEvent` | `BetWon` | Wallet BC (credit winnings), Notifications BC |
| `BetLostIntegrationEvent` | `BetLost` | Wallet BC (consume reservation), Notifications BC |

---

## Phase 4 — Wallet BC

> **Goal:** own user balances and participate in the PlaceBet saga
> as the balance reservation and credit/debit authority.

### Wallet BC — Ubiquitous Language

| Term | Meaning |
|---|---|
| **Wallet** | A user's balance account. One per user. |
| **Reservation** | A temporary hold on a portion of the wallet's available balance, tied to a specific bet. |
| **Available Balance** | Total balance minus all active reservations. |
| **Credit** | Adding funds to a wallet (winnings, top-up). |
| **Debit** | Removing funds from a wallet (consuming a reservation on a lost bet). |

### Aggregates

#### `Wallet`

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Same as `UserId` |
| `TotalBalance` | `decimal` | All funds including reserved |
| `AvailableBalance` | `decimal` | `TotalBalance` minus sum of active reservations |

**Business rules:**
- Available balance cannot go below zero.
- A reservation can only be made if `AvailableBalance >= reservationAmount`.
- A reservation is tied to exactly one bet — identified by `BetId`.
- Crediting winnings increases `TotalBalance` and removes the associated reservation.
- Consuming a reservation (lost bet) reduces `TotalBalance` and removes the reservation.
- A wallet is created the first time a user interacts with the system (lazy creation).

**Domain events:**

| Event | Raised when |
|---|---|
| `WalletCreated` | First interaction by a new user. |
| `BalanceReserved` | A reservation was successfully created. |
| `ReservationReleased` | A reservation was released without consuming funds (cancelled bet). |
| `ReservationConsumed` | A reservation was consumed — funds deducted (lost bet). |
| `BalanceCredited` | Funds were added to the wallet (won bet or top-up). |

#### `BalanceReservation` (sub-aggregate or entity within `Wallet`)

One reservation per active bet. Prevents contention on the `Wallet` aggregate root for high-throughput scenarios.

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Same as `BetId` |
| `Amount` | `decimal` | Held amount |
| `Status` | `enum` | `Active → Released / Consumed` |

**Integration events published (cross-BC):**

| Integration event | Triggered by | Consumed by |
|---|---|---|
| `BalanceReservedIntegrationEvent` | `BalanceReserved` | Betting BC (confirm the bet) |
| `InsufficientFundsIntegrationEvent` | reservation refused | Betting BC (cancel the bet) |

---

## Phase 5 — Notifications BC

> **Goal:** a pure event-reactor service with no domain model of its own.
> Sends push notifications, emails, or in-app messages in response to events from all other BCs.

### Notifications BC — Ubiquitous Language

| Term | Meaning |
|---|---|
| **Notification** | A message delivered to a user via an external channel (push, email, SMS). |
| **Channel** | The delivery mechanism (push notification, email, SMS). |
| **Preference** | A user's opt-in/opt-out setting per notification type and channel. |

### Design principles

- **No aggregates.** Notifications BC has no write model. It reacts to events and performs side effects.
- **No cross-BC commands.** It never tells another BC to do something. It only observes.
- **Idempotency.** Every notification delivery must be idempotent — duplicate events must not produce duplicate sends.
- **User preferences** are read from a local read model populated by a future Users BC or managed locally.

### Events consumed (cross-BC)

| Integration event | Source BC | Notification sent |
|---|---|---|
| `GroupCompletedIntegrationEvent` | Simulator | "Your group has finished. See final standings." |
| `TournamentCompletedIntegrationEvent` | Simulator | "The tournament is over. View results." |
| `BetConfirmedIntegrationEvent` | Betting | "Your bet has been confirmed." |
| `BetCancelledIntegrationEvent` | Betting | "Your bet was cancelled — insufficient funds." |
| `BetWonIntegrationEvent` | Betting | "Congratulations! You won your bet." |
| `BetLostIntegrationEvent` | Betting | "Your bet did not win this time." |

---

## Integration Event Master Table

All integration events that cross BC boundaries, for reference.

| Integration event | Published by | Consumed by |
|---|---|---|
| `TeamRegisteredIntegrationEvent` | Teams | Simulator |
| `TeamStrengthUpdatedIntegrationEvent` | Teams | Simulator |
| `RoundCompletedIntegrationEvent` | Simulator | Betting, Notifications |
| `GroupCompletedIntegrationEvent` | Simulator | Betting, Notifications |
| `TournamentCompletedIntegrationEvent` | Simulator | Notifications |
| `BetInitiatedIntegrationEvent` | Betting | Wallet |
| `BetConfirmedIntegrationEvent` | Betting | Notifications |
| `BetCancelledIntegrationEvent` | Betting | Wallet, Notifications |
| `BetWonIntegrationEvent` | Betting | Wallet, Notifications |
| `BetLostIntegrationEvent` | Betting | Wallet, Notifications |
| `BalanceReservedIntegrationEvent` | Wallet | Betting |
| `InsufficientFundsIntegrationEvent` | Wallet | Betting |

---

## Business Rules That Span Multiple BCs

These rules are enforced by the choreography of integration events, not by a single service.

| Rule | Enforced by |
|---|---|
| A user cannot bet more than their available balance | Wallet BC refuses the reservation → Betting BC cancels the bet |
| A bet cannot be placed after its window has closed | Betting BC rejects the command if `BettingWindow.Status != Open` |
| A team's strength change never retroactively affects an in-progress group | Simulator BC snapshots `TeamInfo` at group creation; ignores subsequent `TeamStrengthUpdatedIntegrationEvent` for live groups |
| A tournament cannot be reported complete until all its groups are complete | Simulator BC tracks `GroupsCompleted` counter on the `Tournament` aggregate |
| Winnings are only credited after the group result is final | Settlement only runs after `BettingWindowClosed`, which is only triggered by `RoundCompletedIntegrationEvent` |

---

## Open Questions Per Phase

### Phase 1 — Teams BC
- Should teams be soft-deleted (deactivated) or hard-deleted? Hard-delete breaks the event stream for any group that used the team.
- Who can update a team's strength — only an admin API, or is it derived from historical match performance?

### Phase 2 — Simulator BC (Rounds + Tournament)
- Should `RoundCompleted` carry a full standings snapshot or just the round number and match results? Full snapshot simplifies consumers but increases message size.
- Should a `Tournament` aggregate live in the Simulator BC (same service) or become its own BC? Extract only if tournament lifecycle becomes a separate team concern.
- What is the maximum number of groups a single tournament should support before batch group creation becomes a domain rule vs. a configuration concern?

### Phase 3 — Betting BC
- Should a user be able to have multiple open bets on the same window but for different predictions? (e.g., bet on team A finishing 1st AND team B finishing 2nd in the same group)
- Should round bets and final-standings bets be different aggregate types or different prediction value objects on the same `Bet` aggregate?
- What are the odds model? Fixed odds (set at window open time) or parimutuel (calculated from total pool at close)?

### Phase 4 — Wallet BC
- Is `BalanceReservation` an entity within `Wallet` (same stream) or a separate aggregate (separate stream)? Separate streams reduce write contention under high bet volume.
- Should top-ups be handled by the Wallet BC directly via an API, or as a reaction to a payment event from an external Payments BC?

### Phase 5 — Notifications BC
- Where are user notification preferences stored — locally in Notifications BC or in a future Users BC?
- Should failed notification deliveries be retried by the messaging pipeline (existing retry middleware) or by the notification service itself?
