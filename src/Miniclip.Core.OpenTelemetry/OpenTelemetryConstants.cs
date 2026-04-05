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
}
