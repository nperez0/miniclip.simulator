# Phase 1 — EventStoreDB: Core Abstractions

> **Status:** ✅ Done  
> **Branch:** `feat/phase-1-esdb-abstractions`  
> **Depends on:** Phase 0 complete  
> **Must not break:** all existing tests must pass at the end of this phase

---

## Goal

Introduce the EventStoreDB container to the Aspire AppHost and lay down all the
event-sourcing abstractions. **No existing write or read behaviour changes.**
The application still persists aggregate state via EF Core at the end of this phase.

This phase is complete and serves as the implementation record for Phase 1.

---

## Why Each Change Exists

| Change | Reason |
|---|---|
| `AggregateRoot` gains `Version` | Needed for optimistic concurrency on the ESDB stream (Phase 2) |
| `AggregateRoot` gains `Apply` | Called during stream replay to reconstruct aggregate state (Phase 2) |
| `AggregateRoot.Enqueue` calls `Apply` | Ensures state is updated the same way during a command and during replay |
| `AggregateRoot.ReplayEvent` | Called by `EventStoreDbEventStore` when loading an aggregate from its event stream |
| `Queue<object>` → `Queue<IDomainEvent>` | Type-safe; `IDomainEvent` is the contract for all domain events |
| `Miniclip.Core.EventSourcing` (new) | Abstractions live in a dependency-free layer; not tied to any infrastructure |
| `Miniclip.Core.EventSourcing.EventStoreDB` (new) | ESDB-specific infrastructure isolated from domain and application layers |
| EventStoreDB in AppHost | Container needs to be available from Phase 2 onward |

---

## Files Changed (overview)

| File | Action |
|---|---|
| `Directory.Packages.props` | Add two new package versions |
| `Miniclip.Core.Domain\AggregateRoot.cs` | Evolve with `Version`, `Apply`, `ReplayEvent`, type-safe queue |
| `Miniclip.Core.Application\Behaviors\DomainEventPublisherBehavior.cs` | Update comment only (return type of `DequeueUncommittedEvents` changes) |
| `Miniclip.Simulator.AppHost\Program.cs` | Add EventStoreDB container |
| `Miniclip.Core.EventSourcing\` *(new project)* | 4 new files |
| `Miniclip.Core.EventSourcing.EventStoreDB\` *(new project)* | 4 new files |

---

## Step-by-Step Implementation

Execute the steps **in the order listed**. Each step is self-contained and
buildable before moving to the next.

---

### Step 1 — Add new packages to central package management

**File:** `Directory.Packages.props`

Add the following two `<PackageVersion>` entries inside the existing `<ItemGroup>`,
maintaining alphabetical order with the other `Microsoft.*` entries:

```xml
<PackageVersion Include="EventStore.Client.Grpc.Streams" Version="23.3.3" />
<PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
```

**Full resulting file:**

```xml
<Project>
  <PropertyGroup>
    <!-- Enable central package management, https://learn.microsoft.com/en-us/nuget/consume-packages/Central-Package-Management -->
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Asp.Versioning.Abstractions" Version="8.1.0" />
    <PackageVersion Include="Asp.Versioning.Http" Version="8.1.1" />
    <PackageVersion Include="Asp.Versioning.Mvc" Version="8.1.0" />
    <PackageVersion Include="Asp.Versioning.Mvc.ApiExplorer" Version="8.1.0" />
    <PackageVersion Include="Aspire.Hosting.AppHost" Version="13.1.2" />
    <PackageVersion Include="Aspire.Hosting.MySql" Version="13.1.2" />
    <PackageVersion Include="AutoFixture" Version="4.18.1" />
    <PackageVersion Include="AutoFixture.AutoNSubstitute" Version="4.18.1" />
    <PackageVersion Include="AutoFixture.NUnit3" Version="4.18.1" />
    <PackageVersion Include="EventStore.Client.Grpc.Streams" Version="23.3.3" />
    <PackageVersion Include="Shouldly" Version="4.3.0" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.5" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.14" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.14" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="9.0.14" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.14" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyModel" Version="9.0.7" />
    <PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="10.4.0" />
    <PackageVersion Include="Microsoft.Extensions.ServiceDiscovery" Version="10.4.0" />
    <PackageVersion Include="Mediator.Abstractions" Version="3.0.1" />
    <PackageVersion Include="Mediator.SourceGenerator" Version="3.0.1" />
    <PackageVersion Include="Scriban" Version="6.6.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageVersion Include="NSubstitute" Version="5.1.0" />
    <PackageVersion Include="NUnit" Version="3.14.0" />
    <PackageVersion Include="NUnit3TestAdapter" Version="5.2.0" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.15.0" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.15.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.1" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.15.0" />
    <PackageVersion Include="Pomelo.EntityFrameworkCore.MySql" Version="9.0.0" />
    <PackageVersion Include="Scalar.AspNetCore" Version="2.13.11" />
  </ItemGroup>
</Project>
```

---

### Step 2 — Evolve `AggregateRoot`

**File:** `Miniclip.Core.Domain\AggregateRoot.cs`  
**Change type:** Edit existing file

Key changes from the current file:
- `Queue<object>` → `Queue<IDomainEvent>`
- `Enqueue(object)` → `Enqueue(IDomainEvent)` — also calls `Apply` before queuing
- `DequeueUncommittedEvents()` return type: `object[]` → `IDomainEvent[]`
- New property: `Version` (`long`, default `-1` means "no events committed yet")
- New method: `ReplayEvent(IDomainEvent, long)` — called during stream replay, does NOT queue
- New method: `Apply(IDomainEvent)` — `protected virtual`, no-op default (overridden in Phase 2)

**Full resulting file:**

```csharp
using System.Text.Json.Serialization;

namespace Miniclip.Core.Domain;

public abstract class AggregateRoot
{
    public Guid Id { get; protected set; }

    /// <summary>
    /// The last event stream position committed to the event store.
    /// -1 means the aggregate has never been persisted (new stream).
    /// Used for optimistic concurrency on append.
    /// </summary>
    public long Version { get; private set; } = -1;

    [JsonIgnore]
    private readonly Queue<IDomainEvent> uncommittedEvents = new();

    public IDomainEvent[] DequeueUncommittedEvents()
    {
        var events = uncommittedEvents.ToArray();

        uncommittedEvents.Clear();

        return events;
    }

    /// <summary>
    /// Raised during normal command processing.
    /// Calls Apply to update in-memory state, then queues the event for persistence.
    /// </summary>
    protected void Enqueue(IDomainEvent @event)
    {
        Apply(@event);
        uncommittedEvents.Enqueue(@event);
    }

    /// <summary>
    /// Called by the event store when replaying historical events to reconstruct state.
    /// Does NOT queue the event — it is already persisted.
    /// </summary>
    public void ReplayEvent(IDomainEvent @event, long version)
    {
        Apply(@event);
        Version = version;
    }

    /// <summary>
    /// Override in each aggregate to apply an event and mutate state.
    /// No-op by default — concrete aggregates implement this in Phase 2.
    /// </summary>
    protected virtual void Apply(IDomainEvent @event) { }
}
```

> **Impact on existing code:**  
> `Group.SimulateMatch` calls `Enqueue(new MatchPlayed(...))`. `MatchPlayed` implements
> `IDomainEvent`, so this still compiles. `Apply` is a no-op in Phase 1, so no state
> mutation occurs beyond what EF Core already manages. All existing tests pass unchanged.

---

### Step 3 — Update `DomainEventPublisherBehavior` comment

**File:** `Miniclip.Core.Application\Behaviors\DomainEventPublisherBehavior.cs`  
**Change type:** Remove stale comment only

The `DequeueUncommittedEvents()` return type changes from `object[]` to `IDomainEvent[]`.
This means `@event` is now typed as `IDomainEvent` (which is `INotification`).
Mediator's `IPublisher` accepts both `object` and `INotification`; the compiler
will now prefer the more specific `INotification` overload. No functional change.

Remove the `// Publish via MediatR` comment (this is no longer MediatR, it's Mediator).

**Full resulting file:**

```csharp
using Mediator;
using Miniclip.Core.Application.Extensions;
using Miniclip.Core.Domain;

namespace Miniclip.Core.Application.Behaviors;

public class DomainEventPublisherBehavior<TRequest, TResponse>(IPublisher publisher, IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(request, cancellationToken);

        if (!request.IsCommand() || !response.IsSuccessful())
            return response;

        var aggregates = unitOfWork.GetTrackedAggregates();
        var events = aggregates.SelectMany(a => a.DequeueUncommittedEvents());

        foreach (var @event in events)
            await publisher.Publish(@event, cancellationToken);

        return response;
    }
}
```

---

### Step 4 — Create `Miniclip.Core.EventSourcing` project

This project holds **abstractions only** — no infrastructure dependencies.  
It lives at the same level as `Miniclip.Core.Domain`.

#### 4a. Project file

**New file:** `Miniclip.Core.EventSourcing\Miniclip.Core.EventSourcing.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Miniclip.Core.Domain\Miniclip.Core.Domain.csproj" />
  </ItemGroup>

</Project>
```

#### 4b. `EventEnvelope`

**New file:** `Miniclip.Core.EventSourcing\EventEnvelope.cs`

Wraps a domain event with the metadata needed to store and identify it in the event store.

```csharp
using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing;

/// <summary>
/// Wraps a domain event with store metadata.
/// Used when reading events back from the event store.
/// </summary>
public sealed record EventEnvelope(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    long Version,
    DateTimeOffset OccurredOn,
    IDomainEvent Data);
```

#### 4c. `IEventSerializer`

**New file:** `Miniclip.Core.EventSourcing\IEventSerializer.cs`

Serialises a domain event to bytes and deserialises bytes back to a domain event.
The abstraction is transport-neutral — no ESDB types leak in.

```csharp
using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing;

public interface IEventSerializer
{
    (string EventType, byte[] Data) Serialize(IDomainEvent @event);

    IDomainEvent Deserialize(string eventType, byte[] data);
}
```

#### 4d. `IEventStore`

**New file:** `Miniclip.Core.EventSourcing\IEventStore.cs`

The primary event-sourcing repository contract.  
- `AppendAsync` — appends the uncommitted events of an aggregate to its stream.  
- `LoadAsync` — replays the stream and returns a fully-reconstructed aggregate.

```csharp
using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing;

public interface IEventStore<T> where T : AggregateRoot
{
    Task AppendAsync(T aggregate, CancellationToken cancellationToken = default);

    Task<T?> LoadAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}
```

---

### Step 5 — Create `Miniclip.Core.EventSourcing.EventStoreDB` project

This project contains the concrete EventStoreDB implementation of the abstractions
defined in Step 4. It lives at the same level as `Miniclip.Core.EF`.

#### 5a. Project file

**New file:** `Miniclip.Core.EventSourcing.EventStoreDB\Miniclip.Core.EventSourcing.EventStoreDB.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="EventStore.Client.Grpc.Streams" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Miniclip.Core.EventSourcing\Miniclip.Core.EventSourcing.csproj" />
  </ItemGroup>

</Project>
```

#### 5b. `SystemTextJsonEventSerializer`

**New file:** `Miniclip.Core.EventSourcing.EventStoreDB\SystemTextJsonEventSerializer.cs`

Serialises events using `System.Text.Json`. The `knownEventTypes` parameter is a
list of all `IDomainEvent` concrete types registered at startup — used for
deserialization by type name.

```csharp
using System.Text.Json;
using Miniclip.Core.Domain;
using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.EventSourcing.EventStoreDB;

public sealed class SystemTextJsonEventSerializer : IEventSerializer
{
    private readonly Dictionary<string, Type> typeMap;

    public SystemTextJsonEventSerializer(IEnumerable<Type> knownEventTypes)
    {
        typeMap = knownEventTypes.ToDictionary(t => t.Name);
    }

    public (string EventType, byte[] Data) Serialize(IDomainEvent @event)
    {
        var eventType = @event.GetType().Name;
        var data = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType());
        return (eventType, data);
    }

    public IDomainEvent Deserialize(string eventType, byte[] data)
    {
        if (!typeMap.TryGetValue(eventType, out var type))
            throw new InvalidOperationException($"Unknown event type: '{eventType}'.");

        return (IDomainEvent)JsonSerializer.Deserialize(data, type)!;
    }
}
```

#### 5c. `EventStoreDbEventStore`

**New file:** `Miniclip.Core.EventSourcing.EventStoreDB\EventStoreDbEventStore.cs`

Implements `IEventStore<T>` using the EventStoreDB gRPC client.

- **Stream name** follows the pattern `{aggregate-type-lowercase}-{aggregate-id}`.
- **Optimistic concurrency**: new streams use `StreamState.NoStream`;
  existing streams use `StreamRevision.FromInt64(aggregate.Version)`.
- **Reconstruction**: creates a blank aggregate via non-public parameterless
  constructor (to be added to each aggregate in Phase 2), then calls
  `ReplayEvent` for each stored event.

> ⚠️ **Note for Phase 2:** `LoadAsync` uses `Activator.CreateInstance(typeof(T), nonPublic: true)`.
> Each aggregate (`Group`, `Team`) must expose a `private` or `protected` parameterless
> constructor in Phase 2 for this to work at runtime.

```csharp
using EventStore.Client;
using Miniclip.Core.Domain;
using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.EventSourcing.EventStoreDB;

public sealed class EventStoreDbEventStore<T>(
    EventStoreClient client,
    IEventSerializer serializer) : IEventStore<T>
    where T : AggregateRoot
{
    private static string StreamName(Guid aggregateId)
        => $"{typeof(T).Name.ToLowerInvariant()}-{aggregateId}";

    public async Task AppendAsync(T aggregate, CancellationToken cancellationToken = default)
    {
        var events = aggregate.DequeueUncommittedEvents();
        if (events.Length == 0)
            return;

        var streamName = StreamName(aggregate.Id);
        var eventData = events.Select(@event =>
        {
            var (eventType, data) = serializer.Serialize(@event);
            return new EventData(Uuid.NewUuid(), eventType, data);
        });

        if (aggregate.Version == -1)
            await client.AppendToStreamAsync(
                streamName,
                StreamState.NoStream,
                eventData,
                cancellationToken: cancellationToken);
        else
            await client.AppendToStreamAsync(
                streamName,
                StreamRevision.FromInt64(aggregate.Version),
                eventData,
                cancellationToken: cancellationToken);
    }

    public async Task<T?> LoadAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var streamName = StreamName(aggregateId);

        var result = client.ReadStreamAsync(
            Direction.Forwards,
            streamName,
            StreamPosition.Start,
            cancellationToken: cancellationToken);

        if (await result.ReadState == ReadState.StreamNotFound)
            return null;

        var aggregate = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
        long version = -1;

        await foreach (var resolvedEvent in result)
        {
            var domainEvent = serializer.Deserialize(
                resolvedEvent.Event.EventType,
                resolvedEvent.Event.Data.ToArray());

            version = (long)resolvedEvent.Event.EventNumber.ToUInt64();
            aggregate.ReplayEvent(domainEvent, version);
        }

        return version == -1 ? null : aggregate;
    }
}
```

#### 5d. `ServiceCollectionExtensions`

**New file:** `Miniclip.Core.EventSourcing.EventStoreDB\Extensions\ServiceCollectionExtensions.cs`

Registers the EventStoreDB client, the serializer, and the generic `IEventStore<>` in DI.
Called from the API's `Startup` / `Program.cs` in Phase 2.

```csharp
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.EventSourcing.EventStoreDB.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventStoreDb(
        this IServiceCollection services,
        string connectionString,
        IEnumerable<Type> knownEventTypes)
    {
        var settings = EventStoreClientSettings.Create(connectionString);

        services.AddSingleton(new EventStoreClient(settings));
        services.AddSingleton<IEventSerializer>(new SystemTextJsonEventSerializer(knownEventTypes));
        services.AddSingleton(typeof(IEventStore<>), typeof(EventStoreDbEventStore<>));

        return services;
    }
}
```

---

### Step 6 — Add EventStoreDB to AppHost

**File:** `Miniclip.Simulator.AppHost\Program.cs`  
**Change type:** Add EventStoreDB container before `builder.Build().Run()`

- Single-node, insecure (TLS off) — development only.
- HTTP/gRPC port `2113` is the Admin UI and gRPC endpoint.
- A named data volume persists events across container restarts.
- No `WithReference` to the API project yet — that wiring happens in Phase 2.

**Full resulting file:**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var mysqlPassword = builder.AddParameter("mysql-password", secret: true);

var mysql = builder.AddMySql("mysql", password: mysqlPassword, port: 3306)
    .WithDataVolume();

var writeDb = mysql.AddDatabase("SimulatorWrite", "MiniclipSimulator_Write");
var readDb = mysql.AddDatabase("SimulatorRead", "MiniclipSimulator_Read");

builder.AddContainer("eventstore", "eventstore/eventstore")
    .WithImageTag("24.10-bookworm-slim")
    .WithEnvironment("EVENTSTORE_CLUSTER_SIZE", "1")
    .WithEnvironment("EVENTSTORE_RUN_PROJECTIONS", "All")
    .WithEnvironment("EVENTSTORE_START_STANDARD_PROJECTIONS", "true")
    .WithEnvironment("EVENTSTORE_INSECURE", "true")
    .WithEnvironment("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", "true")
    .WithHttpEndpoint(port: 2113, targetPort: 2113, name: "http")
    .WithDataVolume("eventstore-data");

builder.AddProject<Projects.Miniclip_Simulator_Api>("simulator-api")
    .WithReference(writeDb)
    .WithReference(readDb)
    .WaitFor(writeDb)
    .WaitFor(readDb);

builder.Build().Run();
```

---

### Step 7 — Register the new projects in the solution

Run from the `src/` directory:

```pwsh
dotnet sln add Miniclip.Core.EventSourcing\Miniclip.Core.EventSourcing.csproj
dotnet sln add Miniclip.Core.EventSourcing.EventStoreDB\Miniclip.Core.EventSourcing.EventStoreDB.csproj
```

---

### Step 8 — Build and verify

```pwsh
dotnet build
dotnet test
```

Both must succeed with zero errors and all existing tests passing.

---

## Definition of Done

- [ ] `dotnet build` succeeds with zero errors  
- [ ] `dotnet test` passes — all existing tests green, none removed  
- [ ] `Miniclip.Core.EventSourcing` project exists with `IEventStore`, `IEventSerializer`, `EventEnvelope`  
- [ ] `Miniclip.Core.EventSourcing.EventStoreDB` project exists and compiles against `EventStore.Client.Grpc.Streams`  
- [ ] `AggregateRoot` has `Version`, `Apply`, `ReplayEvent`, and type-safe `IDomainEvent` queue  
- [ ] EventStoreDB container defined in AppHost (visible in Aspire dashboard when running)  

---

## On Completion

When this phase is done, update **`AI.md`** (and mirror to `.github/copilot-instructions.md`):

```
| 1 | EventStoreDB — Core Abstractions | ✅ Done |
```

And update **`Current Phase`** to:

```
**Current Phase:** `2 — EventStoreDB: Write Side Migration` ⬜
```
