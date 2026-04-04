using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Kafka.OpenTelemetry;

namespace Miniclip.Core.Kafka;

public abstract partial class KafkaConsumerService(
    IKafkaConsumerConfig config,
    IKafkaConsumerFactory consumerFactory,
    IConsumerRetryPolicy retryPolicy,
    ITelemetryRecorderFactory telemetryRecorderFactory,
    ILogger logger) : BackgroundService
{
    protected IKafkaConsumerConfig Config => config;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var effectiveConsumerCount = ResolveConsumerCount(stoppingToken);
        await LogConsumerStatusAsync(stoppingToken);

        var consumers = Enumerable
            .Range(0, effectiveConsumerCount)
            .Select(_ => consumerFactory.CreateConsumer(config, HandleMessageAsync))
            .ToList();

        await Task.WhenAll(consumers.Select(c => c.ConsumeAsync(stoppingToken)));
    }

    protected virtual Task OnDeadLetterAsync(
        ConsumeResult<string, byte[]> result,
        Exception exception,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected abstract Task HandleAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken);

    protected virtual async Task HandleMessageAsync(KafkaMessageContext context, CancellationToken stoppingToken)
    {
        var attempt = 0;
        using var recorder = telemetryRecorderFactory.Create(context, config.ConsumerGroupId);

        while (true)
        {
            try
            {
                await HandleAsync(context.Message, stoppingToken);
                break;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (attempt < retryPolicy.MaxAttempts - 1)
            {
                attempt++;
                recorder.RecordRetryAttempt();
                LogRetryAttempt(logger, ex, attempt, retryPolicy.MaxAttempts, context.Message.Topic);
                await Task.Delay(retryPolicy.Delay(attempt), stoppingToken);
            }
            catch (Exception ex)
            {
                recorder.SetErrorStatus(ex);
                recorder.RecordMessageFailed();
                LogMessagePermanentlyFailed(logger, ex, retryPolicy.MaxAttempts, context.Message.Topic);
                await OnDeadLetterAsync(context.Message, ex, stoppingToken);
                break;
            }
        }

        recorder.RecordProcessingDuration();
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

    protected virtual async Task LogConsumerStatusAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(config.ConsumerConfig).Build();

            var topicPartitions = config.Topics
                .SelectMany(t => adminClient.GetMetadata(t, TimeSpan.FromSeconds(10)).Topics)
                .Where(t => t.Error.Code == ErrorCode.NoError)
                .SelectMany(t => t.Partitions.Select(p => new TopicPartition(t.Topic, p.PartitionId)))
                .ToList();

            if (topicPartitions.Count == 0)
                return;

            var committedResult = await adminClient.ListConsumerGroupOffsetsAsync(
                [new ConsumerGroupTopicPartitions(config.ConsumerGroupId, topicPartitions)]);

            var topicPartitionsOffsetSpecs = topicPartitions
                .Select(tp => new TopicPartitionOffsetSpec() { TopicPartition = tp, OffsetSpec = OffsetSpec.Latest() });
            var endOffsetsResult = await adminClient.ListOffsetsAsync(topicPartitionsOffsetSpecs);

            var endOffsetMap = endOffsetsResult
                .ResultInfos
                .ToDictionary(
                    r => r.TopicPartitionOffsetError.TopicPartition,
                    r => r.TopicPartitionOffsetError.Offset);

            foreach (var tpo in committedResult.SelectMany(g => g.Partitions))
            {
                var endOffset = endOffsetMap.TryGetValue(tpo.TopicPartition, out var e) ? e : Offset.Unset;
                var lag = !endOffset.IsSpecial && !tpo.Offset.IsSpecial
                    ? endOffset.Value - tpo.Offset.Value
                    : -1L;

                LogConsumerPartitionStatus(logger, config.ConsumerGroupId, tpo.Topic, tpo.Partition.Value,
                    tpo.Offset.Value, endOffset.Value, lag);
            }
        }
        catch (Exception ex)
        {
            LogConsumerLagQueryFailed(logger, ex, config.ConsumerGroupId);
        }
    }

    [LoggerMessage(LogLevel.Warning,
        "ConsumerGroup {consumerGroup}: ConsumerCount {requested} exceeds partition count {partitionCount}")]
    static partial void LogConsumerCountClamped(ILogger logger, string consumerGroup, int requested, int partitionCount);

    [LoggerMessage(LogLevel.Warning, "Topic not yet available: {topics}. Retrying in {delaySeconds}s")]
    static partial void LogTopicNotAvailable(ILogger logger, string topics, int delaySeconds);

    [LoggerMessage(LogLevel.Information,
        "ConsumerGroup {ConsumerGroupId} - {Topic}[{Partition}]: currentOffset={CurrentOffset} endOffset={EndOffset} lag={Lag}")]
    static partial void LogConsumerPartitionStatus(ILogger logger, string ConsumerGroupId, string Topic, int Partition,
        long CurrentOffset, long EndOffset, long Lag);

    [LoggerMessage(LogLevel.Warning, "Failed to query consumer lag for group {ConsumerGroupId}")]
    static partial void LogConsumerLagQueryFailed(ILogger logger, Exception ex, string ConsumerGroupId);

    [LoggerMessage(LogLevel.Warning, "Retry {Attempt}/{MaxAttempts} for message from {Topic}")]
    static partial void LogRetryAttempt(ILogger logger, Exception ex, int Attempt, int MaxAttempts, string Topic);

    [LoggerMessage(LogLevel.Error, "Message permanently failed after {MaxAttempts} attempts from {Topic}")]
    static partial void LogMessagePermanentlyFailed(ILogger logger, Exception ex, int MaxAttempts, string Topic);
}

