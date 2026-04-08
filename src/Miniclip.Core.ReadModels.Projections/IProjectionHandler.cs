using Miniclip.Core.Domain;

namespace Miniclip.Core.ReadModels.Projections;

public interface IProjectionHandler
{
    Type EventType { get; }
    int Priority { get; }
    ValueTask HandleAsync(IDomainEvent @event, CancellationToken ct);
}

public interface IProjectionHandler<TEvent> where TEvent : IDomainEvent
{
    ValueTask HandleAsync(TEvent @event, CancellationToken ct);
}
