using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Miniclip.Core.Kafka;

public class KafkaConsumerFactory(
    ConsumerBuilder<string, byte[]> consumerBuilder,
    IKafkaConsumerConfig config,
    ILogger<KafkaConsumer> logger) : IKafkaConsumerFactory
{
    public IKafkaConsumer CreateConsumer(Func<KafkaMessageContext, CancellationToken, Task> onHandleAsync)
        => new KafkaConsumer(consumerBuilder.Build(), config, onHandleAsync, logger);
}