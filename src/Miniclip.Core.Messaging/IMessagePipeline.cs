namespace Miniclip.Core.Messaging;

public interface IMessagePipeline
{
    Task<PipelineResult> ProcessAsync(
        IMessageEnvelope envelope,
        string subscriptionId,
        CancellationToken cancellationToken);
}
