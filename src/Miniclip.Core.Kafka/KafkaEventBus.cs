using Confluent.Kafka;
using Miniclip.Core.Messaging;
using Miniclip.Core.EventSourcing;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Miniclip.Core.Kafka;

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
                new Header("event-type", Encoding.UTF8.GetBytes(eventType)),
                new Header("event-id", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())),
                new Header("occurred-on", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")))
            ]
        };

        await producer.ProduceAsync(topic, message, cancellationToken);
    }
}
