using Confluent.Kafka;

namespace Miniclip.Core.Kafka;

public class KafkaMessageContext(ConsumeResult<string, byte[]> result)
{
    public ConsumeResult<string, byte[]> Result { get; } = result;
}
