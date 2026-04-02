using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Miniclip.Core.Kafka;

public abstract class KafkaConsumerService(
    string[] topics,
    IConfiguration configuration,
    ILogger logger,
    IConsumerRetryPolicy retryPolicy) : BackgroundService
{
    private static readonly TimeSpan TopicConsumerDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ConsumeExceptionDelay = TimeSpan.FromSeconds(5);

    protected abstract string ConsumerGroupId { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration.GetConnectionString("kafka"),
            GroupId = ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var consumer = BuildConsumer(config);
        consumer.Subscribe(topics);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, byte[]> result;
            try
            {
                result = consumer.Consume(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                logger.LogWarning("Topic not yet available: {Topics}. Retrying in {delay}s", topics, TopicConsumerDelay.TotalSeconds);
                await Task.Delay(TopicConsumerDelay, stoppingToken);
                continue;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error consuming from {Topics}", topics);
                await Task.Delay(ConsumeExceptionDelay, stoppingToken);
                continue;
            }

            var attempt = 0;
            while (true)
            {
                try
                {
                    await HandleAsync(result, stoppingToken);
                    consumer.Commit(result);
                    break;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) when (attempt < retryPolicy.MaxAttempts - 1)
                {
                    attempt++;
                    logger.LogWarning(ex, "Retry {Attempt}/{Max} for message from {Topics}", attempt, retryPolicy.MaxAttempts, topics);
                    await Task.Delay(retryPolicy.Delay(attempt), stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Message permanently failed after {Max} attempts from {Topics}", retryPolicy.MaxAttempts, topics);
                    await OnDeadLetterAsync(result, ex, stoppingToken);
                    consumer.Commit(result);
                    break;
                }
            }
        }
    }

    protected virtual IConsumer<string, byte[]> BuildConsumer(ConsumerConfig config)
        => new ConsumerBuilder<string, byte[]>(config).Build();

    protected virtual Task OnDeadLetterAsync(
        ConsumeResult<string, byte[]> result,
        Exception exception,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected abstract Task HandleAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken);
}

