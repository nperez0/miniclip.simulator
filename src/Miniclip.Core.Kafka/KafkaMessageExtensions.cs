using Confluent.Kafka;
using System.Text;

namespace Miniclip.Core.Kafka;

public static class KafkaMessageExtensions
{
    public static string GetHeader(this ConsumeResult<string, byte[]> result, string key)
        => Encoding.UTF8.GetString(result.Message.Headers.GetLastBytes(key));
}
