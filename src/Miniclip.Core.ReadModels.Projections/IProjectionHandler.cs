
namespace Miniclip.Core.ReadModels.Projections;

public interface IProjectionHandler
{
    Type EventType { get; }
    int Priority { get; }
    ValueTask HandleAsync(IIntegrationEvent @event, CancellationToken cancellationToken);
}

public interface IProjectionHandler<in TEvent> where TEvent : IIntegrationEvent
{
    ValueTask HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
