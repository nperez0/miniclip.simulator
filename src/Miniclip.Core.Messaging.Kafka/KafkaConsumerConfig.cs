
namespace Miniclip.Core.Messaging.Kafka;

public sealed class KafkaConsumerConfig : IKafkaConsumerConfig
{
    public required string BootstrapServers { get; init; }
    public required ConsumerGroup ConsumerGroup { get; init; }
    public required string[] Topics { get; init; }
    public int ConsumerCount { get; init; } = 1;

    public ConsumerConfig ConsumerConfig => new()
    {
        BootstrapServers = BootstrapServers,
        GroupId = ConsumerGroup.Id,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false
    };
}
