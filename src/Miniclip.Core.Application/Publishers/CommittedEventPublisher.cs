using System.Diagnostics;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.Messaging;

namespace Miniclip.Core.Application.Publishers;

public sealed class CommittedEventPublisher(IEventBus eventBus) : ICommittedEventPublisher
{
    public async Task PublishAsync(CommittedEvent committed, CancellationToken cancellationToken = default)
    {
        var headers = new Dictionary<string, string>
        {
            [MessageHeaders.EventId]          = committed.EventId.ToString(),
            [MessageHeaders.EventType]        = committed.Event.GetType().Name,
            [MessageHeaders.OccurredOn]       = committed.OccurredOn.ToString("O"),
            [MessageHeaders.AggregateId]      = committed.AggregateId.ToString(),
            [MessageHeaders.AggregateType]    = committed.AggregateType,
            [MessageHeaders.AggregateVersion] = committed.AggregateVersion.ToString(),
        };

        await eventBus.PublishAsync(committed.Event, headers, cancellationToken);
    }
}
