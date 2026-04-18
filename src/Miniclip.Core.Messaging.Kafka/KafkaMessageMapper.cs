using System.Text;
using Confluent.Kafka;

namespace Miniclip.Core.Messaging.Kafka;

public static class KafkaMessageMapper
{
    public static MessageEnvelope ToEnvelope(ConsumeResult<string, byte[]> result)
    {
        var headers = result.Message.Headers
            .ToDictionary(
                h => h.Key,
                h => Encoding.UTF8.GetString(h.GetValueBytes()));

        var messageId = headers.GetValueOrDefault(MessageHeaders.EventId) ?? Guid.NewGuid().ToString();
        var messageType = headers.GetValueOrDefault(MessageHeaders.EventType) ?? "Unknown";
        var occurredOn = headers.GetValueOrDefault(MessageHeaders.OccurredOn) ?? result.Message.Timestamp.UtcDateTime.ToString("o");
        var brokerTimestamp = result.Message.Timestamp.UtcDateTime.ToString("o");

        // Stamp the origin topic and broker timestamp so consumers have full context via headers
        headers[KafkaConstants.Headers.OriginTopic] = result.Topic;
        headers[MessageHeaders.OccurredOn] = occurredOn;
        headers[KafkaConstants.Headers.BrokerTimestamp] = brokerTimestamp;

        return new MessageEnvelope(
            MessageId: messageId,
            MessageType: messageType,
            Payload: result.Message.Value,
            Headers: headers);
    }
}
