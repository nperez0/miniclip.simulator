using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Messaging.Pipeline.Inbound;

namespace Miniclip.Core.Messaging.Kafka;

public sealed partial class KafkaConsumerHost(
    KafkaConsumerDescriptor descriptor,
    ConsumerBuilder<string, string> consumerBuilder,
    IInboundPipeline pipeline,
    IMessageHandlerRegistry registry,
    IDeadLetterHandler deadLetterHandler,
    ILogger<KafkaConsumerHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = consumerBuilder.Build();
        consumer.Subscribe(descriptor.Topics);

        LogStarted(logger, descriptor.Subscription.SubscriptionId, descriptor.Topics);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);

                if (consumeResult.IsPartitionEOF)
                    continue;

                var envelope = KafkaMessageMapper.ToEnvelope(consumeResult);

                if (registry.TryGet(envelope.MessageType) is null)
                {
                    consumer.Commit(consumeResult);
                    LogSkipped(logger, envelope.MessageType, descriptor.Subscription.SubscriptionId);
                    continue;
                }

                var result = await pipeline.ProcessAsync(
                    envelope,
                    descriptor.Subscription.SubscriptionId,
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
                LogConsumeError(logger, ex, descriptor.Subscription.SubscriptionId);
            }
        }

        consumer.Close();
        LogStopped(logger, descriptor.Subscription.SubscriptionId);
    }

    [LoggerMessage(LogLevel.Information,
        "Kafka consumer {ConsumerGroup} started, subscribing to topics: {Topics}")]
    static partial void LogStarted(
        ILogger logger, string ConsumerGroup, string[] Topics);

    [LoggerMessage(LogLevel.Information,
        "Message {MessageId} committed")]
    static partial void LogCommitted(
        ILogger logger, string MessageId);

    [LoggerMessage(LogLevel.Information,
        "Kafka consumer {ConsumerGroup} stopped")]
    static partial void LogStopped(
        ILogger logger, string ConsumerGroup);

    [LoggerMessage(LogLevel.Warning,
        "Message type '{MessageType}' skipped — no handler registered in consumer group {ConsumerGroup}")]
    static partial void LogSkipped(
        ILogger logger, string MessageType, string ConsumerGroup);

    [LoggerMessage(LogLevel.Error,
        "Kafka consume error in consumer group {ConsumerGroup}")]
    static partial void LogConsumeError(
        ILogger logger, Exception ex, string ConsumerGroup);
}
