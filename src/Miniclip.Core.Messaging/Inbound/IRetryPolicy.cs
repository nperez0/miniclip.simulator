namespace Miniclip.Core.Messaging.Inbound;

public interface IRetryPolicy
{
    int MaxAttempts { get; }

    TimeSpan GetDelay(int attemptNumber);
}
