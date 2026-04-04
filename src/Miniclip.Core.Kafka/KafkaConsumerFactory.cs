using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Miniclip.Core.Kafka;

public class KafkaConsumerFactory(ILogger<KafkaConsumer> logger) : IKafkaConsumerFactory
{
    public IKafkaConsumer CreateConsumer(
        IKafkaConsumerConfig config,
        Func<KafkaMessageContext, CancellationToken, Task> onHandleAsync)
        => new KafkaConsumer(config, onHandleAsync, logger);
}