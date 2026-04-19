using Confluent.Kafka;

namespace Miniclip.Core.Messaging.Kafka;

public interface IKafkaConsumerConfig
{
    ConsumerGroup ConsumerGroup { get; }
    string[] Topics { get; }
    int ConsumerCount { get; }
    ConsumerConfig ConsumerConfig { get; }
}
