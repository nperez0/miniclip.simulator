using System.Diagnostics.Metrics;

namespace Miniclip.Core.OpenTelemetry;

public static class OpenTelemetryMetrics
{
    private static readonly Meter Meter = new(OpenTelemetryConstants.Metrics.SimulatorMetricName);

    public static void RecordRetryAttempt()
    {
        RetryAttempts.Add(1);
    }

    public static void RecordMessageFailed()
    {
        MessagesFailed.Add(1);
    }

    private static readonly Counter<long> RetryAttempts =
        Meter.CreateCounter<long>(
            "kafka.retry.attempts",
            unit: "attempts",
            description: "Total number of message processing retry attempts");

    private static readonly Counter<long> MessagesFailed =
        Meter.CreateCounter<long>(
            "kafka.messages.failed",
            unit: "messages",
            description: "Total number of messages that permanently failed after all retries");
}
