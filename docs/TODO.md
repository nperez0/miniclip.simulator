# TODO List

## Projects & Ideas
- [ ] Crate a message context and pass it downstream to the projections
- [ ] How to trace events end-to-end across services
- [ ] Add more information to the events, e.g. correlation ID, causation ID, timestamp, etc.
- [ ] Improve retry policy — could use Polly for more advanced strategies
- [ ] Implement dead-letter queue (persist failed messages)
- [ ] The current IEventBus.PublishAsync takes a CommittedEvent, which is an EventStoreDB concept. Should we have a more generic event message that can be used across different bus implementations? Maybe something like `EventMessage { string Type; string Payload; }`.
- [ ] Create messaging logic that uses AWS SQS/SNS
- [ ] Can we prevent receiving events that a read model is not interested in? Maybe using a different topic per aggregate? The current IEventBus should be renamed to IEventPublisher, and we should add an IEventSubscriber for the projections consumer
- [ ] Review read models strategy
- [ ] Implement a caching strategy (e.g. Redis via decorator pattern) — just to show how it works
- [ ] Add an e2e test project
- [ ] Prepare project to be deployed on Azure or AWS
- [ ] What could be a strategy for service discovery?
- [ ] Check LaunchDarkly for feature flags
- [ ] How does a migration work using KurrentDB?
- [ ] How can read models be rebuilt by replaying events from the beginning?
- [ ] Create a separate project to manage teams
- [ ] Check anti-patterns
- [ ] Make other domain aggregates react to domain events
- [ ] Separate write requests from query requests?
- [ ] Check performance on the write side
- [ ] Graceful shutdown
- [ ] Add useful OpenTelemetry tags and metrics (e.g. domain error counters, conflict rate)
- [ ] Continue improving `Result<T>`
- [ ] There some elements in Miniclip.Core.Application that I am not sure they should be there

## In Progress
- [ ] 

## Completed
- [x] Add health checks to the API (`/health` and `/alive` endpoints)
- [x] Add OpenTelemetry tracing and metrics (custom `Miniclip.Simulator.Kafka` meter, retry/fail counters)
- [x] Add Serilog structured logging with OTLP sink (`Miniclip.Core.ServiceDefaults`)
- [x] Migrate write store from EF Core to EventStoreDB / KurrentDB
- [x] Migrate event bus from in-process Mediator to Kafka
- [x] Move projections to Kafka consumers (`ProjectionsConsumerService<TAggregate>`)
- [x] Separate projections into a standalone ReadModels WebJob
- [x] Add retry policy with exponential back-off (`ExponentialBackoffRetryPolicy`)
- [x] Fix per-aggregate consumer group IDs (`simulator-projections-{aggregate}`)
- [x] Per-partition consumer scaling (`ResolveConsumerCount`)
- [x] Remove `ReadModelUnitOfWorkBehavior` from write pipeline
- [x] Add `LoggingBehavior` to Mediator pipeline
- [x] Add idempotency via `ProcessedEvents` table
- [x] Auto-create Kafka topics on startup (AppHost `WithTopicCreation()`)
- [x] Add integration test project for projections
- [x] Add health checks to the ReadModels WebJob project
- [x] Avoid inheriting from `INotification` for domain events

## Notes
- EventStoreDB was rebranded to **KurrentDB**; client library is `KurrentDB.Client`, Docker image is `kurrentplatform/kurrentdb`.
- Topic naming convention: `simulator.{aggregate-kebab-case}` (e.g. `simulator.group`).
- Consumer group naming: `simulator-projections-{aggregate}` (e.g. `simulator-projections-group`).
