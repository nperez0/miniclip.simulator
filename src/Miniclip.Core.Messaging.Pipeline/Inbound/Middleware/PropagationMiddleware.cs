
namespace Miniclip.Core.Messaging.Pipeline.Inbound.Middleware;

public sealed class PropagationMiddleware(IMutablePropagationContext propagationContext) : IInboundMiddleware
{
    public async Task<MessageHandlerResult> InvokeAsync(
        IMessageEnvelope envelope,
        IMessageContext context,
        Func<Task<MessageHandlerResult>> next,
        CancellationToken cancellationToken)
    {
        if (context.Headers.TryGetValue(MessageHeaders.CorrelationId, out var correlationStr)
            && Guid.TryParse(correlationStr, out var correlationId))
        {
            propagationContext.CorrelationId = correlationId;
        }

        if (context.Headers.TryGetValue(MessageHeaders.CausationId, out var causationStr)
            && Guid.TryParse(causationStr, out var causationId))
        {
            propagationContext.CausationId = causationId;
        }
        else
        {
            // Default: causation equals correlation when not explicitly provided.
            propagationContext.CausationId = propagationContext.CorrelationId;
        }

        return await next();
    }
}
