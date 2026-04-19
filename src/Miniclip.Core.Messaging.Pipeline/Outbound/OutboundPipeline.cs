using Miniclip.Core.Messaging.Outbound;

namespace Miniclip.Core.Messaging.Pipeline.Outbound;

public sealed class OutboundPipeline(
    IEnumerable<IOutboundMiddleware> middlewares, 
    IEventDispatcher dispatcher) 
    : IEventBus
{
    private readonly IOutboundMiddleware[] middlewares = middlewares.ToArray();

    public async Task PublishAsync(object @event, IReadOnlyDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var envelope = new OutboundEnvelope(@event, GetDefaultHeaders(headers));

        var pipeline = () => dispatcher.DispatchAsync(envelope, cancellationToken);

        foreach (var middleware in middlewares.Reverse())
        {
            var next = pipeline;
            var current = middleware;
            pipeline = () => current.InvokeAsync(envelope, next, cancellationToken);
        }

        await pipeline();
    }

    private static Dictionary<string, string> GetDefaultHeaders(IReadOnlyDictionary<string, string>? headers)
    {
        var envelopeHeaders = headers?.ToDictionary() ?? new Dictionary<string, string>();

        envelopeHeaders[MessageHeaders.MessageId] = Guid.NewGuid().ToString();

        return envelopeHeaders;
    }
}
