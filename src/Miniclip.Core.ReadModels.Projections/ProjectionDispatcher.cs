using Miniclip.Core.Messaging;

namespace Miniclip.Core.ReadModels.Projections;

public sealed class ProjectionDispatcher : IProjectionDispatcher
{
    private readonly ILookup<Type, IProjectionHandler> byEventType;

    public ProjectionDispatcher(IEnumerable<IProjectionHandler> handlers)
    {
        byEventType = handlers.OrderBy(h => h.Priority).ToLookup(h => h.EventType);
    }

    public async ValueTask DispatchAsync(IIntegrationEvent @event, CancellationToken cancellationToken)
    {
        var handlers = byEventType[@event.GetType()];

        foreach (var handler in handlers) 
            await handler.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
    }
}
