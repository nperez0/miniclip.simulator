using Miniclip.Core.Messaging.Inbound;

namespace Miniclip.Core.Messaging.Pipeline.Configuration;

public sealed class InboundPipelineOptions
{
    internal IRetryPolicy RetryPolicy { get; private set; } = new ExponentialBackoffRetryPolicy(maxAttempts: 3);

    public InboundPipelineOptions UseRetryPolicy(IRetryPolicy policy)
    {
        RetryPolicy = policy;
        return this;
    }
}
