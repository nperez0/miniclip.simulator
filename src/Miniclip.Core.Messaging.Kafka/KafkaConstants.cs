namespace Miniclip.Core.Messaging.Kafka;

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
