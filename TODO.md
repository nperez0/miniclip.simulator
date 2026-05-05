# TODO List

## Projects & Ideas
- [ ] Outbox pattern for reliable message publishing
- [ ] In the deadletter handler copy all headers and properties of the original message to the new one, so we don't lose any information that could be useful for debugging
- [ ] Review configuration, kafka configuration looks a bit messy
- [ ] Improve retry policy — could use Polly for more advanced strategies
- [ ] Create messaging logic that uses AWS SQS/SNS
- [ ] Review read models strategy
- [ ] Implement a caching strategy (e.g. Redis via decorator pattern) — just to show how it works
- [ ] Add an e2e test project
- [ ] Prepare project to be deployed on Azure or AWS
- [ ] What could be a strategy for service discovery?
- [ ] Check LaunchDarkly for feature flags
- [ ] How does a migration work using KurrentDB?
- [ ] How can read models be rebuilt by replaying events from the beginning?
- [ ] Check anti-patterns
- [ ] Check magic string anti-pattern
- [ ] Separate write requests from query requests?
- [ ] Check performance on the write side
- [ ] Graceful shutdown
- [ ] Add useful OpenTelemetry tags and metrics (e.g. domain error counters, conflict rate)
- [ ] Continue improving `Result<T>`
- [ ] There some elements in Miniclip.Core.Application that I am not sure they should be there
- [ ] Study using of mappers (e.g. AutoMapper, Mapster) in the context of DDD and CQRS. Should we use them to map between domain models, DTOs, and read models, or should we have explicit mapping logic in each layer? What are the trade-offs in terms of maintainability, performance, and testability?
- [ ] Ask about core projects, should some things live in the infrastructure project?

## In Progress
- [ ] Can we prevent receiving events that a read model is not interested in? Maybe using a different topic per aggregate?

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
- [x] Implement dead-letter queue (persist failed messages)
- [x] Is it ok to have the projections consumer directly consume from Kafka, or should we have an intermediate service that reads from Kafka and then calls the projection methods? The current implementation couples the projections to Kafka, which might not be ideal if we want to switch to a different messaging system in the future.
- [x] Create a message context and pass it downstream
- [x] Add more information to the events, e.g. correlation ID, causation ID, timestamp, etc.
- [x] How to trace events end-to-end across services
- [x] The current IEventBus.PublishAsync takes a CommittedEvent, which is an EventStoreDB concept. Should we have a more generic event message that can be used across different bus implementations?
- [x] The serializer should be one for the event bus, not for the event store. The event store should just store byte arrays, and the event bus should be responsible for serializing/deserializing events. This would allow us to use different serializers for different buses if needed.
- [x] Use integration events for communication between services, and domain events for internal communication within a service. This would help to decouple the internal domain model from the external contracts, and allow us to evolve them independently.
- [x] Add global usings

## Notes
- EventStoreDB was rebranded to **KurrentDB**; client library is `KurrentDB.Client`, Docker image is `kurrentplatform/kurrentdb`.
- Topic naming convention: `simulator.{aggregate-kebab-case}` (e.g. `simulator.group`).
- Consumer group naming: `simulator-projections-{aggregate}` (e.g. `simulator-projections-group`).
