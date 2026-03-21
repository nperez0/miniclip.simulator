# ADR-003 - Domain events dispatched post-commit

Status: **Superseded by EventStoreDB + Kafka (Phase 3/4)** | Date: 2026-02

## Original Decision

`IUnitOfWork.CommitAsync()` committed EF Core changes and then dispatched domain events in-process as `INotification` through Mediator. Projections ran synchronously in the same HTTP request.

## How this changed

`IUnitOfWork` and `ReadModelUnitOfWorkBehavior` were removed in Phases 2–4. The post-commit guarantee is now provided by the EventStoreDB + Kafka pipeline:

1. `EventStoreCommandBehavior` calls `IEventStoreSession.CommitAsync()` — appends events to EventStoreDB atomically.
2. `DomainEventPublisherBehavior` publishes committed events to Kafka **only after** the ESDB append succeeds.
3. `ProjectionsConsumerService<TEvent>` consumes each event from Kafka and updates the read DB in a separate transaction, with idempotency via the `ProcessedEvents` table.

## Consequences

- Projections are now **eventually consistent** (asynchronous Kafka consumer) instead of synchronous.
- At-least-once delivery is safe because of the idempotency check.
- `[HandlerPriority]` ordering is still respected within a single consumed message.
- If the Kafka consumer is down, the read DB temporarily lags but self-heals on recovery.
