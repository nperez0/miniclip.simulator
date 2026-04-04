namespace Miniclip.Core.Kafka.OpenTelemetry;

public interface ITelemetryRecorderFactory
{
    ITelemetryRecorder Create(KafkaMessageContext context, string consumerGroupId);
}
