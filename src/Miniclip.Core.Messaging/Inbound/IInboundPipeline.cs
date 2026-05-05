namespace Miniclip.Core.Messaging.Inbound;

public interface IInboundPipeline
{
    Task<PipelineResult> ProcessAsync(
        IMessageEnvelope envelope,
        string subscriptionId,
        CancellationToken cancellationToken);
}
