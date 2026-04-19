namespace Miniclip.Core.Messaging.Inbound;

public sealed class ExponentialBackoffRetryPolicy(
    int maxAttempts = 3,
    int initialDelayMs = 100,
    double backoffMultiplier = 2.0,
    int maxDelayMs = 30000) : IRetryPolicy
{
    public int MaxAttempts { get; } = maxAttempts;

    public TimeSpan GetDelay(int attemptNumber)
    {
        var delayMs = (long)(initialDelayMs * Math.Pow(backoffMultiplier, attemptNumber - 1));
        var capped = Math.Min(delayMs, maxDelayMs);
        return TimeSpan.FromMilliseconds(capped);
    }
}
