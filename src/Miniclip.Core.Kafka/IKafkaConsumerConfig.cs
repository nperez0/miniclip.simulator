using Confluent.Kafka;

namespace Miniclip.Core.Kafka;

public interface IKafkaConsumerConfig
{
    string BootstrapServers { get; }
    string ConsumerGroupId { get; }
    string[] Topics { get; }
    int ConsumerCount { get; }
    ConsumerConfig ConsumerConfig { get; }
}
