using Miniclip.Core.Extensions;
using System.Diagnostics;

namespace Miniclip.Core.Messaging.Pipeline.Outbound.Middleware;

public sealed class PropagationEnrichmentMiddleware(IPropagationContext propagationContext) : IOutboundMiddleware
{
    public async Task InvokeAsync(OutboundEnvelope envelope, Func<Task> next, CancellationToken cancellationToken)
    {
        envelope.Headers[MessageHeaders.CorrelationId] = propagationContext.CorrelationId.ToString();
        envelope.Headers[MessageHeaders.CausationId] = propagationContext.CausationId.ToString();

        var current = Activity.Current;
        if (current is not null)
        {
            envelope.Headers[MessageHeaders.TraceParent] = current.Id!;

            if (current.TraceStateString.IsNotNullOrEmpty())
                envelope.Headers[MessageHeaders.TraceState] = current.TraceStateString;
        }

        await next();
    }
}
