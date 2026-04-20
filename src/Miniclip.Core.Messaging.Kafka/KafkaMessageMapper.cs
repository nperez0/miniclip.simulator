using System.Text;
using Confluent.Kafka;
using Miniclip.Core.Extensions;
using Miniclip.Core.Messaging.Inbound;

namespace Miniclip.Core.Messaging.Kafka;

public static class KafkaMessageMapper
{
    public static MessageEnvelope ToEnvelope(ConsumeResult<string, byte[]> result)
    {
        var headers = result.Message.Headers
            .GroupBy(h => h.Key)
            .ToDictionary(
                h => h.Key,
                h => Encoding.UTF8.GetString(h.First().GetValueBytes()));

        var messageId = headers.GetValueOrDefault(MessageHeaders.MessageId, Guid.NewGuid().ToString());
        var messageType = headers.GetValueOrDefault(MessageHeaders.MessageType, "Unknown");
        var originTimestamp = headers.GetValueOrDefault(MessageHeaders.OriginTimestamp, result.Message.Timestamp.UtcDateTime.ToRoundTripString());
        var brokerTimestamp = result.Message.Timestamp.UtcDateTime.ToRoundTripString();

        // Stamp the origin topic and broker timestamp so consumers have full context via headers
        headers[KafkaConstants.Headers.OriginTopic] = result.Topic;
        headers[MessageHeaders.OriginTimestamp] = originTimestamp;
        headers[KafkaConstants.Headers.BrokerTimestamp] = brokerTimestamp;

        return new MessageEnvelope(
            MessageId: messageId,
            MessageType: messageType,
            Payload: result.Message.Value,
            Headers: headers);
    }
}
