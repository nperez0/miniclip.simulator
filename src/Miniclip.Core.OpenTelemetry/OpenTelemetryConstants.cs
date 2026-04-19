namespace Miniclip.Core.OpenTelemetry;

public static class OpenTelemetryConstants
{
    public static class Metrics
    {
        public const string SimulatorMetricName = "Miniclip.Simulator.Kafka";
    }

    public static class ActivitySources
    {
        public const string SimulatorSourceName = "Miniclip.Simulator";
        public const string MySqlData = "connector-net";
        public const string MySqlConnector = "MySqlConnector";
    }

    public static class Tags
    {
        // Activity names
        public const string MessageProcess = "miniclip.message.process";
        public const string MessagePublish = "miniclip.message.publish";

        // Message attributes
        public const string MessageId = "miniclip.messaging.message_id";
        public const string MessageType = "miniclip.messaging.message_type";
        public const string SubscriptionId = "miniclip.messaging.subscription_id";
        public const string CorrelationId = "miniclip.messaging.correlation_id";
        public const string CausationId = "miniclip.messaging.causation_id";

        // Event sourcing attributes
        public const string AggregateId = "miniclip.event_sourcing.aggregate_id";
        public const string AggregateType = "miniclip.event_sourcing.aggregate_type";
        public const string AggregateVersion = "miniclip.event_sourcing.aggregate_version";
        public const string EventId = "miniclip.event_sourcing.event_id";
    }
}
