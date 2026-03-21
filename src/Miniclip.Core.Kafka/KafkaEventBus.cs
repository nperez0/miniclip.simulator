using System.Text;
using Confluent.Kafka;
using Miniclip.Core.Application;
using Miniclip.Core.Domain;
using Miniclip.Core.EventSourcing;

namespace Miniclip.Core.Kafka;

public sealed class KafkaEventBus(
    IProducer<string, byte[]> producer,
    IEventSerializer serializer) : IEventBus
{
    public async Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default)
    {
        var topic = TopicNaming.For(@event);
        var (eventType, data) = serializer.Serialize(@event);

        var message = new Message<string, byte[]>
        {
            Key = @event.AggregateId.ToString(),
            Value = data,
            Headers =
            [
                new Header("event-type",   Encoding.UTF8.GetBytes(eventType)),
                new Header("event-id",     Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())),
                new Header("occurred-on",  Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")))
            ]
        };

        await producer.ProduceAsync(topic, message, cancellationToken);
    }
}
