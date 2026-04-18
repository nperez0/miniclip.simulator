using Confluent.Kafka;

namespace Miniclip.Core.Messaging.Kafka;

public interface IKafkaConsumerConfig
{
    string ConsumerGroupId { get; }
    string[] Topics { get; }
    int ConsumerCount { get; }
    ConsumerConfig ConsumerConfig { get; }
}
