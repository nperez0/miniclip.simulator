using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Messaging.Inbound;
using Miniclip.Core.Messaging.Outbound;

namespace Miniclip.Core.Messaging.Kafka;

public sealed partial class KafkaEventDispatcher(
    IProducer<string, byte[]> producer,
    IMessageSerializer serializer,
    ILogger<KafkaEventDispatcher> logger) : IEventDispatcher
{
    public async Task DispatchAsync(OutboundEnvelope envelope, CancellationToken cancellationToken)
    {
        var eventType = envelope.Event.GetType().Name;
        var aggregateType = envelope.Headers.GetValueOrDefault(MessageHeaders.AggregateType, eventType);
        var topic = TopicNaming.ForAggregate(aggregateType);
        var data = serializer.Serialize(envelope.Event);

        var messageHeaders = new Headers();

        foreach (var (key, value) in envelope.Headers)
            messageHeaders.Add(key, Encoding.UTF8.GetBytes(value));

        var message = new Message<string, byte[]>
        {
            Key = envelope.Headers.GetValueOrDefault(MessageHeaders.AggregateId, Guid.NewGuid().ToString()),
            Value = data,
            Headers = messageHeaders
        };

        await producer.ProduceAsync(topic, message, cancellationToken);

        LogDispatched(logger, eventType, topic);
    }

    [LoggerMessage(LogLevel.Debug, "Dispatched {EventType} to {Topic}")]
    static partial void LogDispatched(ILogger logger, string EventType, string Topic);
}
