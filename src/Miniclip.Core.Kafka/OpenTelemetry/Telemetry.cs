using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Miniclip.Core.Kafka.OpenTelemetry;

internal static class Telemetry
{
    internal const string ActivitySourceName = "Miniclip.Kafka";
    internal const string MeterName = "Miniclip.Kafka";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    internal static readonly Meter Meter = new(MeterName);

    internal static readonly Counter<long> MessagesConsumed =
        Meter.CreateCounter<long>(
            "kafka.messages.consumed",
            unit: "messages",
            description: "Total number of messages consumed from Kafka");

    internal static readonly Counter<long> MessagesFailed =
        Meter.CreateCounter<long>(
            "kafka.messages.failed",
            unit: "messages",
            description: "Total number of messages that permanently failed after all retries");

    internal static readonly Counter<long> RetryAttempts =
        Meter.CreateCounter<long>(
            "kafka.retry.attempts",
            unit: "attempts",
            description: "Total number of message processing retry attempts");

    internal static readonly Histogram<double> ProcessingDuration =
        Meter.CreateHistogram<double>(
            "kafka.processing.duration",
            unit: "ms",
            description: "Duration of message processing in milliseconds");

    internal static TagList Tags(string topic, string consumerGroup) => new()
    {
        { "topic", topic },
        { "consumer_group", consumerGroup }
    };
}
