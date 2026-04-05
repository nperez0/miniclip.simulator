using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Miniclip.Core.OpenTelemetry;

namespace Miniclip.Core.Kafka;

public abstract partial class KafkaConsumerService(
    IKafkaConsumerConfig config,
    IKafkaConsumerFactory consumerFactory,
    IConsumerRetryPolicy retryPolicy,
    ILogger logger) : BackgroundService
{
    protected IKafkaConsumerConfig Config => config;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var effectiveConsumerCount = ResolveConsumerCount(stoppingToken);

        var consumers = Enumerable
            .Range(0, effectiveConsumerCount)
            .Select(_ => consumerFactory.CreateConsumer(HandleMessageAsync))
            .ToList();

        await Task.WhenAll(consumers.Select(c => c.ConsumeAsync(stoppingToken)));
    }

    protected virtual Task OnDeadLetterAsync(
        KafkaMessageContext context,
        Exception exception,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected abstract Task HandleAsync(
        KafkaMessageContext context,
        CancellationToken cancellationToken);

    protected virtual async Task HandleMessageAsync(KafkaMessageContext context, CancellationToken stoppingToken)
    {
        var attempt = 0;
        using var activity = OpenTelemetryActivity.StartActivity(context.Result.GetHeader("event-type"));

        while (true)
        {
            try
            {
                await HandleAsync(context, stoppingToken);
                break;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (attempt < retryPolicy.MaxAttempts - 1)
            {
                attempt++;
                OpenTelemetryMetrics.RecordRetryAttempt();
                LogRetryAttempt(logger, ex, attempt, retryPolicy.MaxAttempts, context.Result.Topic);
                await Task.Delay(retryPolicy.Delay(attempt), stoppingToken);
            }
            catch (Exception ex)
            {
                activity.NoticeError(ex);
                OpenTelemetryMetrics.RecordMessageFailed();
                LogMessagePermanentlyFailed(logger, ex, retryPolicy.MaxAttempts, context.Result.Topic);
                await OnDeadLetterAsync(context, ex, stoppingToken);
                break;
            }
        }
    }

    protected virtual int ResolveConsumerCount(CancellationToken stoppingToken)
    {
        using var adminClient = new AdminClientBuilder(config.ConsumerConfig).Build();

        var partitionCount = config.Topics
            .Select(t => adminClient.GetMetadata(t, TimeSpan.FromSeconds(10)))
            .SelectMany(m => m.Topics)
            .Where(t => t.Error.Code == ErrorCode.NoError)
            .Select(t => t.Partitions.Count)
            .DefaultIfEmpty(1)
            .Min();

        if (config.ConsumerCount <= partitionCount)
            return config.ConsumerCount;

        LogConsumerCountClamped(logger, config.ConsumerGroupId, config.ConsumerCount, partitionCount);

        return partitionCount;
    }

    [LoggerMessage(LogLevel.Warning, "ConsumerGroup {consumerGroup}: ConsumerCount {requested} exceeds partition count {partitionCount}")]
    static partial void LogConsumerCountClamped(ILogger logger, string consumerGroup, int requested, int partitionCount);

    [LoggerMessage(LogLevel.Warning, "Retry {Attempt}/{MaxAttempts} for message from {Topic}")]
    static partial void LogRetryAttempt(ILogger logger, Exception ex, int Attempt, int MaxAttempts, string Topic);

    [LoggerMessage(LogLevel.Error, "Message permanently failed after {MaxAttempts} attempts from {Topic}")]
    static partial void LogMessagePermanentlyFailed(ILogger logger, Exception ex, int MaxAttempts, string Topic);
}

