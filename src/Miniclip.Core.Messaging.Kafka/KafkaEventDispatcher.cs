using System.Text;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Messaging.Outbound;

namespace Miniclip.Core.Messaging.Kafka;

public sealed partial class KafkaEventDispatcher(
    IProducer<string, string> producer,
    IDestinationResolver destinationResolver,
    IMessageSerializer serializer,
    ILogger<KafkaEventDispatcher> logger) : IEventDispatcher
{
    public async Task DispatchAsync(OutboundEnvelope envelope, CancellationToken cancellationToken)
    {
        var topic = destinationResolver.Resolve(envelope);
        var payload = serializer.Serialize(envelope.Event);

        var messageHeaders = new Headers();

        foreach (var (key, value) in envelope.Headers)
            messageHeaders.Add(key, Encoding.UTF8.GetBytes(value ?? string.Empty));

        var message = new Message<string, string>
        {
            Key = envelope.MessageGroupId,
            Value = payload,
            Headers = messageHeaders
        };

        await producer.ProduceAsync(topic, message, cancellationToken);

        LogDispatched(logger, envelope.EventType, topic);
    }

    [LoggerMessage(LogLevel.Debug, "Dispatched {EventType} to {Topic}")]
    static partial void LogDispatched(ILogger logger, string EventType, string Topic);
}
