namespace Miniclip.Core.Messaging.Outbound;

public interface IOutboundMiddleware
{
    Task InvokeAsync(OutboundEnvelope envelope, Func<Task> next, CancellationToken cancellationToken);
}

