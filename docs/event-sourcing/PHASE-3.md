# Phase 3 — Kafka: Event Bus

> **Status:** ✅ Complete
> **Branch:** `feat/phase-3-kafka-bus`
> **Depends on:** Phase 2 complete ✅
> **Must not break:** all existing API behaviour and tests must remain green during the transition

---

## Goal

Replace in-process `IPublisher.Publish` inside `DomainEventPublisherBehavior` with a durable Kafka publish. Domain events committed to EventStoreDB are forwarded to Kafka topics. Projections continue to work unchanged via a temporary Kafka→Mediator relay consumer.

---

## Current State (after Phase 2)

`DomainEventPublisherBehavior` calls `session.GetCommittedEvents()` and publishes each event via `IPublisher.Publish` (in-process Mediator). Projection handlers (`GroupStandingsProjection`, `MatchResultProjection`) are `INotificationHandler<MatchPlayed>` implementations that run synchronously within the same HTTP request pipeline.

```
HTTP request
  └─ EventStoreCommandBehavior     → CommitAsync → events appended to ESDB
  └─ DomainEventPublisherBehavior  → Publish(MatchPlayed) in-process
       └─ GroupStandingsProjection
       └─ MatchResultProjection
  └─ ReadModelUnitOfWorkBehavior   → SaveChanges (MySQL)
```

After Phase 3 the chain becomes:

```
HTTP request
  └─ EventStoreCommandBehavior     → CommitAsync → events appended to ESDB
  └─ DomainEventPublisherBehavior  → PublishAsync(event) → Kafka topic
  └─ ReadModelUnitOfWorkBehavior   → SaveChanges (MySQL, nothing to save yet*)

Kafka consumer (BackgroundService)
  └─ MatchPlayedKafkaRelayService  → Publish(MatchPlayed) via Mediator
       └─ GroupStandingsProjection
       └─ MatchResultProjection    → SaveChanges (MySQL)
```

> \* The `ReadModelUnitOfWorkBehavior` wraps the HTTP pipeline, but in Phase 3 the read models are updated asynchronously by the relay consumer. The behavior is kept in place for future synchronous projections.

---

## Design Decisions

### 1. Add `Guid AggregateId` to `IDomainEvent`

The event bus needs a partition key to guarantee ordering of all events for the same group. Rather than relying on reflection or conventions, `IDomainEvent` gains one property:

```csharp
// Miniclip.Core.Domain/IDomainEvent.cs
public interface IDomainEvent : INotification
{
    Guid AggregateId { get; }
}
```

All four Group events already carry `GroupId`. Each implements the property via explicit interface implementation:

```csharp
public record GroupCreated(Guid GroupId, string Name, int Capacity) : IDomainEvent
{
    Guid IDomainEvent.AggregateId => GroupId;
}
```

This change is additive: `IDomainEvent` gains a required property. All implementors are in the same solution, so the compiler enforces completeness.

### 2. `IEventBus` lives in `Miniclip.Core.Application`

`DomainEventPublisherBehavior` lives in `Core.Application` and must not reference Kafka infrastructure directly. Defining the abstraction in the same project follows Dependency Inversion:

```csharp
// Miniclip.Core.Application/IEventBus.cs
public interface IEventBus
{
    Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default);
}
```

The concrete implementation (`KafkaEventBus`) lives in `Miniclip.Core.Kafka`.

### 3. One topic per event type, named `simulator.{kebab-case}`

| Event | Topic |
|---|---|
| `GroupCreated` | `simulator.group-created` |
| `TeamAdded` | `simulator.team-added` |
| `MatchScheduled` | `simulator.match-scheduled` |
| `MatchPlayed` | `simulator.match-played` |

Topic name is derived at runtime:

```csharp
// Miniclip.Core.Kafka/TopicNaming.cs
public static class TopicNaming
{
    private static readonly Regex PascalCasePattern = new(@"(?<=.)([A-Z])", RegexOptions.Compiled);

    public static string For(IDomainEvent @event)
        => $"simulator.{PascalCasePattern.Replace(@event.GetType().Name, "-$1").ToLowerInvariant()}";
}
```

### 4. Kafka message format: headers + raw payload

The Kafka message value is the JSON-serialized event payload (identical bytes to what EventStoreDB already stores), produced by the existing `IEventSerializer.Serialize`. Metadata travels in message headers:

| Part | Content |
|---|---|
| **Key** | `event.AggregateId.ToString()` (UTF-8) |
| **Value** | `IEventSerializer.Serialize(@event).data` — JSON bytes |
| **Header `event-type`** | event type name, e.g. `"MatchPlayed"` |
| **Header `event-id`** | `Guid.NewGuid().ToString()` (for idempotency in Phase 4+) |
| **Header `occurred-on`** | `DateTimeOffset.UtcNow.ToString("O")` |

The consumer reconstructs the event with `IEventSerializer.Deserialize(eventType, valueBytes)` — same method already used by the EventStoreDB store on replay.

This deliberately mirrors the EventStoreDB storage format so that the same `IEventSerializer` serves both stores without adaptation.

### 5. `Miniclip.Core.Kafka` — new generic project

Contains only infrastructure that is not simulator-specific:

```
Miniclip.Core.Kafka/
├── Miniclip.Core.Kafka.csproj
├── IEventBus.cs                  ← no, IEventBus lives in Core.Application
├── KafkaEventBus.cs              ← implements IEventBus
├── KafkaConsumerService.cs       ← abstract BackgroundService base
├── TopicNaming.cs
└── ServiceCollectionExtensions.cs
```

Project references:
- `Confluent.Kafka` NuGet
- `Miniclip.Core.Application` (for `IEventBus`, `IEventSerializer` proxy via Core.EventSourcing)
- `Miniclip.Core.EventSourcing` (for `IEventSerializer`)

### 6. Relay consumer: temporary Kafka→Mediator bridge in `ReadModels.Projections`

The existing projection handlers do not change in Phase 3. A `MatchPlayedKafkaRelayService` is added to `Miniclip.Simulator.ReadModels.Projections` to consume `simulator.match-played` and re-publish via Mediator:

```csharp
// temporary — removed in Phase 4
public class MatchPlayedKafkaRelayService(
    IEventSerializer serializer,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<MatchPlayedKafkaRelayService> logger)
    : KafkaConsumerService(["simulator.match-played"], configuration, logger)
{
    protected override async Task HandleAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken)
    {
        var eventType = result.GetHeader("event-type");
        var domainEvent = serializer.Deserialize(eventType, result.Message.Value);

        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(domainEvent, cancellationToken);
    }
}
```

> `IServiceScopeFactory` is required because `IPublisher` (Mediator, registered as scoped) cannot be injected into a singleton `BackgroundService` directly.

The consumer creates its own `IConsumer<string, byte[]>` internally (not via Aspire's `AddKafkaConsumer`) to avoid coupling the consumer registration to Aspire's generic singleton pattern, which would prevent Phase 4 from having multiple independent consumers.

Consumer group: `simulator-projections` (same group used in Phase 4 for the dedicated per-handler consumers).

### 7. Aspire Kafka integration (AppHost side)

Use `Aspire.Hosting.Kafka` instead of `AddContainer`:

```csharp
var kafka = builder.AddKafka("kafka")
    .WithKafkaUI();

builder.AddProject<Projects.Miniclip_Simulator_Api>("simulator-api")
    .WithReference(writeDb)
    .WithReference(readDb)
    .WithReference(kafka)       // ← injects connection string
    .WaitFor(writeDb)
    .WaitFor(readDb)
    .WaitFor(kafka);
```

The API project uses `Aspire.Confluent.Kafka` to register `IProducer<string, byte[]>` from the injected connection string:

```csharp
services.AddKafkaProducer<string, byte[]>("kafka");
```

Topic auto-creation is enabled in the Aspire Kafka container by default and is acceptable for local development. Disabling it and pre-declaring topics explicitly is deferred to Phase 5.

---

## Critical Ordering Constraint

The pipeline ordering established in Phase 2 is preserved:

```
ReadModelUnitOfWorkBehavior (outermost)
  └─ DomainEventPublisherBehavior   ← now calls IEventBus.PublishAsync (Kafka)
       └─ EventStoreCommandBehavior (innermost) ← CommitAsync to ESDB first
```

`EventStoreCommandBehavior` commits to ESDB before `DomainEventPublisherBehavior` publishes to Kafka. An event is **never** published to Kafka without first being durably stored in EventStoreDB.

If the Kafka publish fails after ESDB commit, the event is not lost — it remains in ESDB and can be replayed later (Phase 5 hardening).

---

## Projects Affected

| Project | Change |
|---|---|
| `Miniclip.Core.Domain` | Add `Guid AggregateId { get; }` to `IDomainEvent` |
| `Miniclip.Simulator.Domain` | All 4 events implement `IDomainEvent.AggregateId` via explicit interface |
| `Miniclip.Core.Application` | Add `IEventBus`; update `DomainEventPublisherBehavior` to use `IEventBus` instead of `IPublisher` |
| `Miniclip.Core.Kafka` *(new)* | `KafkaEventBus`, `KafkaConsumerService` base, `TopicNaming`, `ServiceCollectionExtensions` |
| `Miniclip.Simulator.ReadModels.Projections` | Add `MatchPlayedKafkaRelayService` (temporary bridge) |
| `Miniclip.Simulator.AppHost` | Add Kafka + Kafka UI; pass Kafka reference to API |
| `Miniclip.Simulator.Api` | `AddKafkaProducer`; register `KafkaEventBus`; register relay consumer |
| `Miniclip.Simulator.Api/appsettings.json` | Add `"kafka"` connection string for non-Aspire runs |
| Various `.csproj` files | `Aspire.Hosting.Kafka`, `Aspire.Confluent.Kafka`, `Confluent.Kafka` package refs |

---

## Implementation Steps

### Step 1 — Add `AggregateId` to `IDomainEvent`

In `Miniclip.Core.Domain/IDomainEvent.cs`, add the property:

```csharp
public interface IDomainEvent : INotification
{
    Guid AggregateId { get; }
}
```

In each of the four Group events, implement via explicit interface:

```csharp
public record GroupCreated(Guid GroupId, string Name, int Capacity) : IDomainEvent
{
    Guid IDomainEvent.AggregateId => GroupId;
}
```

Repeat for `TeamAdded`, `MatchScheduled`, `MatchPlayed`.

> Any future event outside the Group aggregate follows the same pattern — map its aggregate's identity property to `AggregateId`.

### Step 2 — Add `IEventBus` to `Miniclip.Core.Application`

Create `Miniclip.Core.Application/IEventBus.cs`:

```csharp
using Miniclip.Core.Domain;

namespace Miniclip.Core.Application;

public interface IEventBus
{
    Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default);
}
```

Update `DomainEventPublisherBehavior` — replace `IPublisher publisher` constructor parameter with `IEventBus eventBus`:

```csharp
public class DomainEventPublisherBehavior<TRequest, TResponse>(IEventBus eventBus, IEventStoreSession session)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async ValueTask<TResponse> Handle(...)
    {
        var response = await next(request, cancellationToken);

        if (!request.IsCommand() || !response.IsSuccessful())
            return response;

        foreach (var @event in session.GetCommittedEvents())
            await eventBus.PublishAsync(@event, cancellationToken);

        return response;
    }
}
```

### Step 3 — Create `Miniclip.Core.Kafka` project

`Miniclip.Core.Kafka.csproj`:
- `PackageReference Include="Confluent.Kafka"`
- `PackageReference Include="Microsoft.Extensions.Hosting.Abstractions"` (for BackgroundService)
- `PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions"`
- `PackageReference Include="Microsoft.Extensions.Logging.Abstractions"`
- `ProjectReference` → `Miniclip.Core.Application` (for `IEventBus`)
- `ProjectReference` → `Miniclip.Core.EventSourcing` (for `IEventSerializer`)

### Step 4 — Implement `TopicNaming` and `KafkaEventBus`

```csharp
// TopicNaming.cs
public static class TopicNaming
{
    private static readonly Regex PascalCasePattern = new(@"(?<=.)([A-Z])", RegexOptions.Compiled);

    public static string For(IDomainEvent @event)
        => $"simulator.{PascalCasePattern.Replace(@event.GetType().Name, "-$1").ToLowerInvariant()}";
}
```

```csharp
// KafkaEventBus.cs
public sealed class KafkaEventBus(
    IProducer<string, byte[]> producer,
    IEventSerializer serializer) : IEventBus
{
    public async Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default)
    {
        var topic = TopicNaming.For(@event);
        var (eventType, data) = serializer.Serialize(@event);

        var message = new Message<string, byte[]>
        {
            Key = @event.AggregateId.ToString(),
            Value = data,
            Headers = new Headers
            {
                { "event-type", Encoding.UTF8.GetBytes(eventType) },
                { "event-id",   Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
                { "occurred-on", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) }
            }
        };

        await producer.ProduceAsync(topic, message, cancellationToken);
    }
}
```

### Step 5 — Implement `KafkaConsumerService` base

```csharp
// KafkaConsumerService.cs
public abstract class KafkaConsumerService(
    string[] topics,
    IConfiguration configuration,
    ILogger logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration.GetConnectionString("kafka"),
            GroupId = ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe(topics);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                await HandleAsync(result, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { logger.LogError(ex, "Error processing Kafka message"); }
        }

        consumer.Close();
    }

    protected abstract string ConsumerGroupId { get; }

    protected abstract Task HandleAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken);
}
```

Extension method on `ConsumeResult` to extract headers cleanly:

```csharp
// KafkaMessageExtensions.cs
public static string GetHeader(this ConsumeResult<string, byte[]> result, string key)
    => Encoding.UTF8.GetString(result.Message.Headers.GetLastBytes(key));
```

### Step 6 — `ServiceCollectionExtensions` in `Core.Kafka`

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, KafkaEventBus>();
        return services;
    }
}
```

> `IProducer<string, byte[]>` is registered by `services.AddKafkaProducer<string, byte[]>("kafka")` from `Aspire.Confluent.Kafka` in the API project.

### Step 7 — Add `MatchPlayedKafkaRelayService` to `ReadModels.Projections`

Add `PackageReference Include="Aspire.Confluent.Kafka"` (for `IProducer<,>` types — not strictly needed here, but the project now depends on `Miniclip.Core.Kafka`) and `ProjectReference` → `Miniclip.Core.Kafka`.

```csharp
// MatchPlayedKafkaRelayService.cs — temporary, removed in Phase 4
public class MatchPlayedKafkaRelayService(
    IEventSerializer serializer,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<MatchPlayedKafkaRelayService> logger)
    : KafkaConsumerService(["simulator.match-played"], configuration, logger)
{
    protected override string ConsumerGroupId => "simulator-projections";

    protected override async Task HandleAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken)
    {
        var eventType = result.GetHeader("event-type");
        var domainEvent = serializer.Deserialize(eventType, result.Message.Value);

        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(domainEvent, cancellationToken);
    }
}
```

### Step 8 — Update AppHost

Add `Aspire.Hosting.Kafka` package to AppHost. Update `Program.cs`:

```csharp
var kafka = builder.AddKafka("kafka")
    .WithKafkaUI();

builder.AddProject<Projects.Miniclip_Simulator_Api>("simulator-api")
    .WithReference(writeDb)
    .WithReference(readDb)
    .WithReference(kafka)
    .WaitFor(writeDb)
    .WaitFor(readDb)
    .WaitFor(kafka);
```

### Step 9 — Update DI wiring in API

In `DatabaseConfiguration` (or a new `KafkaConfiguration`):

```csharp
services.AddKafkaProducer<string, byte[]>("kafka");  // from Aspire.Confluent.Kafka
services.AddKafkaEventBus();                          // from Core.Kafka extension
services.AddHostedService<MatchPlayedKafkaRelayService>();
```

Add `appsettings.json` fallback for non-Aspire runs (integration tests, manual startup):

```json
"ConnectionStrings": {
    "kafka": "localhost:9092"
}
```

Add project references in `Api.csproj`:
- `Miniclip.Core.Kafka`
- `Miniclip.Simulator.ReadModels.Projections` (to register `MatchPlayedKafkaRelayService`)

> Note: `Miniclip.Simulator.ReadModels.Projections` is already referenced by `Api.csproj` (for projection handler discovery via `AddMediator`).

### Step 10 — Update unit tests

`DomainEventPublisherBehavior` now depends on `IEventBus` instead of `IPublisher`. Update the corresponding unit tests to mock `IEventBus` instead.

Any test that asserts on `IPublisher.Publish` calls must be updated to assert on `IEventBus.PublishAsync`.

---

## Definition of Done

- [x] `IDomainEvent` has `Guid AggregateId { get; }` and all 4 Group events implement it
- [x] `IEventBus` is defined in `Miniclip.Core.Application`
- [x] `DomainEventPublisherBehavior` publishes via `IEventBus` (not `IPublisher`)
- [x] `Miniclip.Core.Kafka` project exists with `KafkaEventBus`, `KafkaConsumerService`, `TopicNaming`
- [x] `MatchPlayedKafkaRelayService` runs in the API as a `BackgroundService`
- [x] Kafka and Kafka UI containers are visible and healthy in the Aspire dashboard
- [x] After simulating a group, `MatchPlayed` events appear in the `simulator.match-played` topic in Kafka UI
- [x] Read models are still correctly populated after simulation (relay consumer works end-to-end)
- [x] All existing unit tests pass (including updated `DomainEventPublisherBehavior` tests)
- [x] Build is green

---

## Out of Scope (deferred to later phases)

| Concern | Deferred To |
|---|---|
| Disabling Kafka topic auto-creation; pre-declaring topics | Phase 5 |
| Idempotency checks in the relay consumer (preventing duplicate read model updates) | Phase 4 |
| Dead-letter topic handling for failed messages | Phase 5 |
| Publishing all 4 event types to their respective topics — only `MatchPlayed` has a consumer now | Phase 4 |
| Outbox pattern to guarantee at-least-once ESDB→Kafka delivery | Phase 5 |
| `Team` event sourcing | Out of scope |

---

## On Completion

When this phase is done, update:

- `docs/event-sourcing/PLAN.md`
- `AI.md`
- `.github/copilot-instructions.md`

Mark Phase 3 as complete and move the current phase to Phase 4.
