namespace Miniclip.Core.Messaging;

public interface IEventBus
{
    Task PublishAsync(
        object @event,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}