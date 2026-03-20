# ADR-002 - CQRS with separate DbContexts

Status: Accepted | Date: 2026-02

## Context

Reads and writes have different shapes. Writes operate on normalised aggregates; reads need flat projections for fast querying.

## Decision

Two separate EF Core DbContexts:
- SimulatorWriteDbContext owns Groups, Teams, Matches.
- SimulatorReadDbContext owns GroupStandings, MatchResults.

The read DB is populated exclusively by projections reacting to domain events. Query handlers never touch the write context.

## Consequences

- Adding a new read model only requires a new model + projection + read migration, without touching the write schema.
- Queries are fast because they hit pre-computed, denormalised tables.
- The read DB can be fully rebuilt by replaying MatchPlayed events if ever lost.
