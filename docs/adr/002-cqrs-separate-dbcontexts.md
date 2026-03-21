# ADR-002 - CQRS with separate write and read stores

Status: Accepted | Date: 2026-02 | Updated: 2026-03

## Context

Reads and writes have different shapes. Writes operate on event-sourced aggregates; reads need flat projections for fast querying.

## Decision

Two separate persistence stores:
- **EventStoreDB** is the write store. All aggregates (`Group`, `Team`) are stored as event streams. There is no EF Core write model for aggregates; `SimulatorWriteDbContext` has an empty model and exists only to carry the migration that dropped legacy aggregate tables.
- **`SimulatorReadDbContext`** (MySQL, EF Core) owns `GroupStandings`, `MatchResults`, and `ProcessedEvents`.

The read DB is populated exclusively by `ProjectionsConsumerService<TEvent>` reacting to Kafka events. Query handlers never touch the write store.

## Consequences

- Adding a new read model requires only a new model + projection handler + read migration.
- Queries are fast because they hit pre-computed, denormalised tables.
- The read DB can be fully rebuilt by replaying events from EventStoreDB through Kafka.
