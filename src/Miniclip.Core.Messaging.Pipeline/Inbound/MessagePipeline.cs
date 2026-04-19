using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Messaging.Inbound;

namespace Miniclip.Core.Messaging.Pipeline.Inbound;

public sealed class MessagePipeline(
    IMessageHandlerRegistry registry,
    IMessageSerializer serializer,
    IServiceScopeFactory scopeFactory,
    ILogger<MessagePipeline> logger) : IInboundPipeline
{
    public async Task<PipelineResult> ProcessAsync(
        IMessageEnvelope envelope,
        string subscriptionId,
        CancellationToken cancellationToken)
    {
        var context = new MessageContext(
            envelope.MessageId,
            subscriptionId,
            envelope.Headers);

        // Create a single scope for the entire message -- middlewares and handler share it.
        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var scopedMiddlewares = services.GetServices<IInboundMiddleware>().ToArray();

        // Build the middleware chain -- first registered is outermost.
        var handler = () => InvokeHandlerAsync(envelope, context, services, cancellationToken);

        foreach (var middleware in scopedMiddlewares.Reverse())
        {
            var next = handler;
            var current = middleware;
            handler = () => current.InvokeAsync(envelope, context, next, cancellationToken);
        }

        var result = await handler();

        return new PipelineResult(
            result.IsSuccess,
            result is { IsSuccess: false, ShouldRetry: false },
            result.ErrorMessage);
    }

    private async Task<MessageHandlerResult> InvokeHandlerAsync(
        IMessageEnvelope envelope,
        IMessageContext context,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        try
        {
            var handler = registry.TryGet(envelope.MessageType);

            if (handler is null)
            {
                var errorMsg = $"No handler registered for message type {envelope.MessageType}";
                logger.LogWarning("{Error}", errorMsg);
                return MessageHandlerResult.PermanentFailure(errorMsg);
            }

            var message = serializer.Deserialize(envelope.MessageType, envelope.Payload);

            var handlerInstance = services.GetRequiredService(handler.HandlerType);

            return await handler.InvokeAsync(handlerInstance, message, context, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error invoking handler for message {MessageId} ({MessageType})",
                envelope.MessageId, envelope.MessageType);
            return MessageHandlerResult.PermanentFailure(ex.Message);
        }
    }
}

internal sealed record MessageContext(
    string MessageId,
    string SubscriptionId,
    IReadOnlyDictionary<string, string> Headers) : IMessageContext;
