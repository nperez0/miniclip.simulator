namespace Miniclip.Core.Messaging;

public interface IEventBus
{
    Task PublishAsync(
        object @event,
        string? messageGroupId = null,
        IReadOnlyDictionary<string, string?>? headers = null,
        CancellationToken cancellationToken = default);
}