using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.Application.Publishers;

/// <summary>
/// Translates a <see cref="CommittedEvent"/> into an agnostic <see cref="Messaging.IEventBus"/> call,
/// building the full header set and tagging the upstream OpenTelemetry activity.
/// </summary>
public interface ICommittedEventPublisher
{
    Task PublishAsync(CommittedEvent committed, CancellationToken cancellationToken = default);
}
