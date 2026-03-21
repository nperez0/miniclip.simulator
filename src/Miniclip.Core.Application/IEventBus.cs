using Miniclip.Core.Domain;

namespace Miniclip.Core.Application;

public interface IEventBus
{
    Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default);
}
