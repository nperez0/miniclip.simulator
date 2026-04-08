using Miniclip.Core.Domain;

namespace Miniclip.Core.ReadModels.Projections;

public interface IProjectionDispatcher
{
    ValueTask DispatchAsync(IDomainEvent @event, CancellationToken ct);
}

public sealed class ProjectionDispatcher : IProjectionDispatcher
{
    private readonly ILookup<Type, IProjectionHandler> _byEventType;
    private readonly IReadOnlyList<IProjectionPipelineBehavior> _behaviors;

    public ProjectionDispatcher(
        IEnumerable<IProjectionHandler> handlers,
        IEnumerable<IProjectionPipelineBehavior> behaviors)
    {
        _byEventType = handlers.OrderBy(h => h.Priority).ToLookup(h => h.EventType);
        _behaviors   = [.. behaviors];
    }

    public ValueTask DispatchAsync(IDomainEvent @event, CancellationToken ct)
    {
        var handlers = _byEventType[@event.GetType()];

        ProjectionHandlerDelegate terminal = async token =>
        {
            foreach (var handler in handlers)
                await handler.HandleAsync(@event, token).ConfigureAwait(false);
        };

        var pipeline = _behaviors
            .Reverse()
            .Aggregate(terminal, (next, behavior) =>
                token => behavior.HandleAsync(@event, token, next));

        return pipeline(ct);
    }
}
