using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Miniclip.Core.Messaging.Pipeline;

public sealed class MessagePipeline(
    IEnumerable<IMessageMiddleware> middlewares,
    IMessageHandlerRegistry registry,
    IMessageSerializer serializer,
    IServiceScopeFactory scopeFactory,
    ILogger<MessagePipeline> logger) : IMessagePipeline
{
    private readonly IMessageMiddleware[] middlewares = middlewares.ToArray();

    public async Task<PipelineResult> ProcessAsync(
        IMessageEnvelope envelope,
        string subscriptionId,
        CancellationToken cancellationToken)
    {
        var context = new MessageContext(
            envelope.MessageId,
            subscriptionId,
            envelope.Headers);

        // Build the middleware chain (innermost = rightmost = last registered)
        // Middleware is invoked in reverse order of registration
        var handler = () => InvokeHandlerAsync(envelope, context, cancellationToken);

        foreach (var middleware in middlewares.Reverse())
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

            using var scope = scopeFactory.CreateScope();
            var handlerInstance = scope.ServiceProvider.GetRequiredService(handler.HandlerType);

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
