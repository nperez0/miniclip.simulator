using Miniclip.Core.Extensions;
using Miniclip.Core.Messaging;

namespace Miniclip.Core.Messaging.Pipeline.Outbound;

public sealed class OutboundPipeline(
    IEnumerable<IOutboundMiddleware> middlewares,
    IEventDispatcher dispatcher)
    : IEventBus
{
    private readonly IOutboundMiddleware[] middlewares = middlewares.ToArray();

    public async Task PublishAsync(
        object @event,
        string? messageGroupId = null,
        IReadOnlyDictionary<string, string?>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = new OutboundEnvelope(@event, messageGroupId, GetDefaultHeaders(@event, messageGroupId, headers));

        var pipeline = () => dispatcher.DispatchAsync(envelope, cancellationToken);

        foreach (var middleware in middlewares.Reverse())
        {
            var next = pipeline;
            var current = middleware;
            pipeline = () => current.InvokeAsync(envelope, next, cancellationToken);
        }

        await pipeline();
    }

    private static Dictionary<string, string?> GetDefaultHeaders(object @event, string? messageGroupId, IReadOnlyDictionary<string, string?>? headers)
    {
        var envelopeHeaders = headers?.ToDictionary() ?? new Dictionary<string, string?>();

        envelopeHeaders[MessageHeaders.MessageId] = Guid.NewGuid().ToString();
        envelopeHeaders[MessageHeaders.MessageType] = @event.GetType().GetMessageTypeName();
        envelopeHeaders[MessageHeaders.MessageGroupId] = messageGroupId;
        envelopeHeaders[MessageHeaders.OriginTimestamp] = DateTimeOffset.UtcNow.ToRoundTripString();

        return envelopeHeaders;
    }
}