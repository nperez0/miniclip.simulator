using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Miniclip.Core.Messaging.Kafka;

public sealed partial class KafkaConsumerHost(
    IKafkaConsumerConfig config,
    ConsumerBuilder<string, byte[]> consumerBuilder,
    IInboundPipeline pipeline,
    IDeadLetterHandler deadLetterHandler,
    ILogger<KafkaConsumerHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = consumerBuilder.Build();
        consumer.Subscribe(config.Topics);

        LogStarted(logger, config.ConsumerGroup.Id, config.Topics);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);

                if (consumeResult.IsPartitionEOF)
                    continue;

                var envelope = KafkaMessageMapper.ToEnvelope(consumeResult);

                if (!pipeline.CanHandle(envelope.MessageType))
                {
                    consumer.Commit(consumeResult);
                    LogSkipped(logger, envelope.MessageType, config.ConsumerGroup.Id);
                    continue;
                }

                var result = await pipeline.ProcessAsync(
                    envelope,
                    config.ConsumerGroup.Id,
                    stoppingToken);

                if (result.ShouldDeadLetter)
                {
                    await deadLetterHandler.HandleAsync(
                        envelope,
                        result.ErrorMessage ?? "Unknown error",
                        exception: null,
                        stoppingToken);
                }

                consumer.Commit(consumeResult);
                LogCommitted(logger, envelope.MessageId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                LogConsumeError(logger, ex, config.ConsumerGroup.Id);
            }
        }

        consumer.Close();
        LogStopped(logger, config.ConsumerGroup.Id);
    }

    [LoggerMessage(LogLevel.Debug,
        "Message type '{MessageType}' skipped — no handler registered in consumer group {ConsumerGroup}")]
    static partial void LogSkipped(
        ILogger logger, string MessageType, string ConsumerGroup);

    [LoggerMessage(LogLevel.Information,
        "Kafka consumer {ConsumerGroup} started, subscribing to topics: {Topics}")]
    static partial void LogStarted(
        ILogger logger, string ConsumerGroup, string[] Topics);

    [LoggerMessage(LogLevel.Information,
        "Message {MessageId} committed")]
    static partial void LogCommitted(
        ILogger logger, string MessageId);

    [LoggerMessage(LogLevel.Error,
        "Kafka consume error in consumer group {ConsumerGroup}")]
    static partial void LogConsumeError(
        ILogger logger, Exception ex, string ConsumerGroup);

    [LoggerMessage(LogLevel.Information,
        "Kafka consumer {ConsumerGroup} stopped")]
    static partial void LogStopped(
        ILogger logger, string ConsumerGroup);
}
