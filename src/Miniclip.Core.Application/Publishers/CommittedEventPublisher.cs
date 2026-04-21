using Miniclip.Core.Application.IntegrationEvents;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Extensions;
using Miniclip.Core.Messaging;

namespace Miniclip.Core.Application.Publishers;

public sealed class CommittedEventPublisher(
    IEventBus eventBus,
    IIntegrationEventMapperRegistry mapperRegistry) : ICommittedEventPublisher
{
    public async Task PublishAsync(CommittedEvent committed, CancellationToken cancellationToken = default)
    {
        var integrationEvent = mapperRegistry.TryMap(committed.Event);

        if (integrationEvent is null)
            return;

        var headers = new Dictionary<string, string?>
        {
            [MessageHeaders.EventId] = committed.EventId.ToString(),
            [MessageHeaders.EventType] = committed.Event.GetType().FullName,
            [MessageHeaders.OccurredOn] = committed.OccurredOn.ToRoundTripString(),
            [MessageHeaders.AggregateId] = committed.AggregateId.ToString(),
            [MessageHeaders.AggregateType] = committed.AggregateType,
            [MessageHeaders.AggregateVersion] = committed.AggregateVersion.ToString(),
        };

        await eventBus.PublishAsync(integrationEvent, committed.AggregateId.ToString(), headers, cancellationToken);
    }
}
