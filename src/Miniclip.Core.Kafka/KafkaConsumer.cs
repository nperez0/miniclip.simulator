using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Miniclip.Core.Kafka;

public partial class KafkaConsumer(
    IConsumer<string, byte[]> consumer,
    IKafkaConsumerConfig config,
    Func<KafkaMessageContext, CancellationToken, Task> onHandleAsync,
    ILogger logger)
    : IKafkaConsumer
{
    private static readonly TimeSpan ConsumeExceptionDelay = TimeSpan.FromSeconds(5);

    public async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(config.Topics);

        LogTopicsSubscribed(logger, string.Join(",", config.Topics));

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, byte[]>? result = null;
            try
            {
                result = consumer.Consume(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                LogConsumeError(logger, ex, string.Join(",", config.Topics));
                await Task.Delay(ConsumeExceptionDelay, stoppingToken);
                continue;
            }

            await onHandleAsync(new KafkaMessageContext(result), stoppingToken);
            consumer.Commit(result);
        }
    }

    [LoggerMessage(LogLevel.Information, "Topics subscribed: {Topics}")]
    static partial void LogTopicsSubscribed(ILogger logger, string Topics);

    [LoggerMessage(LogLevel.Warning, "Topic not yet available: {Topics}. Retrying in {DelaySeconds}s")]
    static partial void LogTopicNotAvailable(ILogger logger, string Topics, int DelaySeconds);

    [LoggerMessage(LogLevel.Error, "Error consuming from {Topics}")]
    static partial void LogConsumeError(ILogger logger, Exception ex, string Topics);
}
