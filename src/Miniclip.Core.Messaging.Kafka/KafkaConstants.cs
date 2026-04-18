namespace Miniclip.Core.Messaging.Kafka;

/// <summary>
/// Kafka-specific configuration constants that are not message header keys.
/// </summary>
public static class KafkaConstants
{
    public static class Headers
    {
        public const string OriginTopic = "kafka-origin-topic";
        public const string BrokerTimestamp = "kafka-broker-timestamp";
    }

    public static class DeadLetter
    {
        public const string TopicSuffix = ".dlq";
        public const string UnknownOriginTopic = "unknown";
    }
}
