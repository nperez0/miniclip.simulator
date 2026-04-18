using System.Diagnostics.Metrics;

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
        // --- Activity names ---
        public const string MessageProcess = "miniclip.message.process";

        // --- Message attributes ---
        public const string MessageId = "miniclip.messaging.message_id";
        public const string MessageType = "miniclip.messaging.message_type";
        public const string SubscriptionId = "miniclip.messaging.subscription_id";
        public const string CorrelationId = "miniclip.messaging.correlation_id";
    }
}
