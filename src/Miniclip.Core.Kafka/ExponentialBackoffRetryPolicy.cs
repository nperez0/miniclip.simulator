namespace Miniclip.Core.Kafka;

public sealed class ExponentialBackoffRetryPolicy(int maxAttempts = 3, TimeSpan? baseDelay = null)
    : IConsumerRetryPolicy
{
    private readonly TimeSpan @base = baseDelay ?? TimeSpan.FromSeconds(1);

    public int MaxAttempts { get; } = maxAttempts;

    public TimeSpan Delay(int attempt)
        => TimeSpan.FromMilliseconds(@base.TotalMilliseconds * Math.Pow(2, attempt - 1));
}
