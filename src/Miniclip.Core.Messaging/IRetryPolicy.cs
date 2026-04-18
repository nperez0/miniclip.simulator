namespace Miniclip.Core.Messaging;

public interface IRetryPolicy
{
    int MaxAttempts { get; }

    TimeSpan GetDelay(int attemptNumber);
}
