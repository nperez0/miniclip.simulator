using Confluent.Kafka;

namespace Miniclip.Core.Kafka;

public class KafkaMessageContext(
    ConsumeResult<string, byte[]> message,
    IKafkaConsumerConfig config)
{
    public ConsumeResult<string, byte[]> Message { get; } = message;
    public IKafkaConsumerConfig Config { get; } = config;
}
