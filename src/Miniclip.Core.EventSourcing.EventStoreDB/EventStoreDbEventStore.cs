using EventStore.Client;
using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing.EventStoreDB;

public sealed class EventStoreDbEventStore<T>(
    EventStoreClient client,
    IEventSerializer serializer,
    IEventStoreSession session) : IEventStore<T>
    where T : AggregateRoot
{
    public void Track(T aggregate)
        => session.Track(ct => AppendAsync(aggregate, ct));

    public async Task<T?> LoadAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var streamName = GetStreamName(aggregateId);
        var result = client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start, cancellationToken: cancellationToken);

        if (await result.ReadState == ReadState.StreamNotFound)
            return null;

        var aggregate = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
        var version = -1L;

        await foreach (var resolvedEvent in result)
        {
            var domainEvent = serializer.Deserialize(
                resolvedEvent.Event.EventType,
                resolvedEvent.Event.Data.ToArray());

            version = (long)resolvedEvent.Event.EventNumber.ToUInt64();
            aggregate.ReplayEvent(domainEvent, version);
        }

        if (version < 0)
            return null;

        Track(aggregate);

        return aggregate;
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categoryStream = $"$ce-{typeof(T).Name.ToLowerInvariant()}";
        var prefix = $"{typeof(T).Name.ToLowerInvariant()}-";

        var result = client.ReadStreamAsync(
            Direction.Forwards,
            categoryStream,
            StreamPosition.Start,
            resolveLinkTos: true,
            cancellationToken: cancellationToken);

        if (await result.ReadState == ReadState.StreamNotFound)
            return [];

        var aggregates = new Dictionary<Guid, T>();

        await foreach (var resolvedEvent in result)
        {
            var streamId = resolvedEvent.Event.EventStreamId;
            var id = Guid.Parse(streamId[prefix.Length..]);

            if (!aggregates.TryGetValue(id, out var aggregate))
            {
                aggregate = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
                aggregates[id] = aggregate;
            }

            var domainEvent = serializer.Deserialize(
                resolvedEvent.Event.EventType,
                resolvedEvent.Event.Data.ToArray());

            var version = (long)resolvedEvent.Event.EventNumber.ToUInt64();
            aggregate.ReplayEvent(domainEvent, version);
        }

        return aggregates.Values;
    }

    private async Task<CommittedEvent[]> AppendAsync(T aggregate, CancellationToken cancellationToken)
    {
        var events = aggregate.DequeueUncommittedEvents();
        if (events.Length == 0)
            return [];

        var streamName = GetStreamName(aggregate.Id);
        var eventData = events
            .Select(ToEventData)
            .ToArray();

        if (aggregate.Version < 0)
        {
            await client.AppendToStreamAsync(streamName, StreamState.NoStream, eventData, cancellationToken: cancellationToken);

            SetVersion(aggregate, eventData.Length - 1);
        }
        else
        {
            await client.AppendToStreamAsync(
                streamName,
                StreamRevision.FromInt64(aggregate.Version),
                eventData,
                cancellationToken: cancellationToken);

            SetVersion(aggregate, aggregate.Version + eventData.Length);
        }

        return events
            .Select(e => new CommittedEvent(e, typeof(T).Name))
            .ToArray();
    }

    private EventData ToEventData(IDomainEvent @event)
    {
        var (eventType, data) = serializer.Serialize(@event);

        return new EventData(Uuid.NewUuid(), eventType, data);
    }

    private static string GetStreamName(Guid aggregateId)
        => $"{typeof(T).Name.ToLowerInvariant()}-{aggregateId}";

    private static void SetVersion(T aggregate, long version)
    {
        typeof(AggregateRoot)
            .GetProperty(nameof(AggregateRoot.Version))
            ?.SetValue(aggregate, version);
    }
}

