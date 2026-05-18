using Microsoft.Extensions.DependencyInjection;

namespace Miniclip.Core.Messaging.Pipeline.Inbound;

public sealed class MessagePipeline(
    IReadOnlyList<Type> middlewareTypes,
    IMessageHandlerRegistry registry,
    IMessageDeserializer deserializer,
    IServiceScopeFactory scopeFactory) : IInboundPipeline
{
    public async Task<PipelineResult> ProcessAsync(
        IMessageEnvelope envelope,
        string subscriptionId,
        CancellationToken cancellationToken)
    {
        var compiled = registry.TryGet(envelope.MessageType);

        if (compiled is null)
            return new PipelineResult(IsSuccess: true, ShouldDeadLetter: false, ErrorMessage: null);

        await using var scope = scopeFactory.CreateAsyncScope();

        var middlewares = middlewareTypes
            .Select(t => (IInboundMiddleware)scope.ServiceProvider.GetRequiredService(t))
            .ToArray();

        var handler = scope.ServiceProvider.GetRequiredService(compiled.HandlerType);
        var message = deserializer.Deserialize(envelope.MessageType, envelope.Payload);
        var context = new MessageContext(envelope.MessageId, subscriptionId, envelope.Headers);

        var pipeline = () => compiled.InvokeAsync(handler, message, context, cancellationToken);

        foreach (var middleware in middlewares.Reverse())
        {
            var next = pipeline;
            var current = middleware;
            pipeline = () => current.InvokeAsync(envelope, context, next, cancellationToken);
        }

        try
        {
            var result = await pipeline();

            return result.IsSuccess
                ? new PipelineResult(IsSuccess: true, ShouldDeadLetter: false, ErrorMessage: null)
                : new PipelineResult(IsSuccess: false, ShouldDeadLetter: true, ErrorMessage: result.ErrorMessage);
        }
        catch (Exception ex)
        {
            return new PipelineResult(IsSuccess: false, ShouldDeadLetter: true, ErrorMessage: ex.Message);
        }
    }
}

file sealed class MessageContext(
    string messageId,
    string subscriptionId,
    IReadOnlyDictionary<string, string> headers) : IMessageContext
{
    public string MessageId { get; } = messageId;
    public string SubscriptionId { get; } = subscriptionId;
    public IReadOnlyDictionary<string, string> Headers { get; } = headers;
}
