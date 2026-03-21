namespace Miniclip.Core.Kafka;

public interface IConsumerRetryPolicy
{
    int MaxAttempts { get; }
    TimeSpan Delay(int attempt);
}
