using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.Messaging.Kafka;

public sealed class KafkaEventBus(
    IProducer<string, byte[]> producer,
    IEventSerializer serializer,
    ILogger<KafkaEventBus> logger) : IEventBus
{
    public async Task PublishAsync(CommittedEvent committed, CancellationToken cancellationToken = default)
    {
        var topic = TopicNaming.ForAggregate(committed.AggregateType);
        var (eventType, data) = serializer.Serialize(committed.Event);

        var message = new Message<string, byte[]>
        {
            Key = committed.Event.AggregateId.ToString(),
            Value = data,
            Headers =
            [
                new Header(MessageHeaders.EventType, Encoding.UTF8.GetBytes(eventType)),
                new Header(MessageHeaders.EventId, Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())),
                new Header(MessageHeaders.OccurredOn, Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")))
            ]
        };

        await producer.ProduceAsync(topic, message, cancellationToken);
    }
}
