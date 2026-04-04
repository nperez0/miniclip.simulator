using Confluent.Kafka;

namespace Miniclip.Core.Kafka;

public sealed class KafkaConsumerConfig : IKafkaConsumerConfig
{
    public required string BootstrapServers { get; init; }
    public required string ConsumerGroupId { get; init; }
    public required string[] Topics { get; init; }
    public int ConsumerCount { get; init; } = 1;

    public ConsumerConfig ConsumerConfig => new()
    {
        BootstrapServers = BootstrapServers,
        GroupId = ConsumerGroupId,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false
    };
}
