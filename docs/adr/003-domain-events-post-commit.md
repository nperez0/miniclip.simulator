# ADR-003 - Domain events dispatched post-commit via IUnitOfWork

Status: Accepted | Date: 2026-02

## Context

Domain events must only be dispatched after the write transaction has committed. Dispatching before commit risks projections processing phantom events.

## Decision

IUnitOfWork.CommitAsync() is responsible for:
1. Saving all EF Core changes to the write DB.
2. Collecting uncommitted events via AggregateRoot.DequeueUncommittedEvents().
3. Dispatching each event as an INotification through the Mediator pipeline.

Projections (INotificationHandler<MatchPlayed>) run in priority order via [HandlerPriority] within the same request.

## Consequences

- Projections always see committed write data.
- [HandlerPriority(1)] (MatchResultProjection) runs before [HandlerPriority(2)] (GroupStandingsProjection).
- If a projection fails after commit, compensating logic or a retry must be added if eventual consistency is not acceptable.
