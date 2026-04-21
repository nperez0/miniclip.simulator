using Miniclip.Core.Messaging;
namespace Miniclip.Core.ReadModels.Projections;

public interface IProjectionDispatcher
{
    ValueTask DispatchAsync(IIntegrationEvent @event, CancellationToken cancellationToken);
}