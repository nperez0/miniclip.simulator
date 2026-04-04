using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Text;

namespace Miniclip.Core.Kafka.OpenTelemetry;

public class KafkaTelemetryRecorderFactory(ILogger<KafkaTelemetryRecorderFactory> logger)
    : ITelemetryRecorderFactory
{
    public ITelemetryRecorder Create(KafkaMessageContext context, string consumerGroupId)
    {
        var topic = context.Message.Topic;
        var partition = context.Message.Partition.Value;
        var offset = context.Message.Offset.Value;
        var tags = Telemetry.Tags(topic, context.Config.ConsumerConfig.GroupId);

        Activity? activity = null;
        try
        {
            Telemetry.MessagesConsumed.Add(1, tags);

            var parentContext = ExtractActivityContext(context);
            activity = Telemetry.ActivitySource.StartActivity(
                $"{topic} process",
                ActivityKind.Consumer,
                parentContext);

            activity?.SetTag("messaging.system", "kafka");
            activity?.SetTag("messaging.operation.type", "process");
            activity?.SetTag("messaging.destination.name", topic);
            activity?.SetTag("messaging.kafka.consumer.group", consumerGroupId);
            activity?.SetTag("messaging.kafka.message.offset", offset);
            activity?.SetTag("messaging.kafka.destination.partition", partition);
            activity?.SetTag("event.id", context.Message.GetHeader("event-id"));
            activity?.SetTag("event.type", context.Message.GetHeader("event-type"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start activity for topic {Topic}", topic);
        }

        return new KafkaMessageContextTelemetry(tags, activity, logger);
    }

    private ActivityContext ExtractActivityContext(KafkaMessageContext context)
    {
        var propagationContext = Propagators.DefaultTextMapPropagator.Extract(
            default,
            context.Message.Message.Headers,
            (headers, key) =>
            {
                try
                {
                    var header = headers.LastOrDefault(h => h.Key == key);
                    return header is not null ? [Encoding.UTF8.GetString(header.GetValueBytes())] : [];
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to extract propagation header {Key}", key);
                    return [];
                }
            });

        Baggage.Current = propagationContext.Baggage;

        return propagationContext.ActivityContext;
    }
}
