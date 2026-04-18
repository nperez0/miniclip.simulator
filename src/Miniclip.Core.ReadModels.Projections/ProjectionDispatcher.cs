using Miniclip.Core.Domain;

namespace Miniclip.Core.ReadModels.Projections;

public interface IProjectionDispatcher
{
    ValueTask DispatchAsync(IDomainEvent @event, CancellationToken ct);
}

public sealed class ProjectionDispatcher : IProjectionDispatcher
{
    private readonly ILookup<Type, IProjectionHandler> byEventType;

    public ProjectionDispatcher(IEnumerable<IProjectionHandler> handlers)
    {
        byEventType = handlers.OrderBy(h => h.Priority).ToLookup(h => h.EventType);
    }

    public async ValueTask DispatchAsync(IDomainEvent @event, CancellationToken ct)
    {
        var handlers = byEventType[@event.GetType()];

        foreach (var handler in handlers) 
            await handler.HandleAsync(@event, ct).ConfigureAwait(false);
    }
}
