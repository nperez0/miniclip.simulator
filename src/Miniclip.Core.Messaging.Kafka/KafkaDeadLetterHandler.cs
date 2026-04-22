using System.Text;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Extensions;

namespace Miniclip.Core.Messaging.Kafka;

public sealed partial class KafkaDeadLetterHandler(
    IProducer<string, byte[]> producer,
    ILogger<KafkaDeadLetterHandler> logger) : IDeadLetterHandler
{
    public async Task HandleAsync(
        IMessageEnvelope envelope,
        string reason,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        var originTopic = envelope.Headers.GetValueOrDefault(KafkaConstants.Headers.OriginTopic)
            ?? KafkaConstants.DeadLetter.UnknownOriginTopic;
        var dlqTopic = $"{originTopic}{KafkaConstants.DeadLetter.TopicSuffix}";

        var headers = new Headers
        {
            { MessageHeaders.OriginalCorrelationId, Encoding.UTF8.GetBytes(envelope.Headers.GetValueOrDefault(MessageHeaders.CorrelationId, string.Empty)) },
            { MessageHeaders.OriginalMessageId, Encoding.UTF8.GetBytes(envelope.MessageId) },
            { MessageHeaders.OriginalMessageType, Encoding.UTF8.GetBytes(envelope.MessageType) },
            { MessageHeaders.FailureReason, Encoding.UTF8.GetBytes(reason) },
            { MessageHeaders.FailedAt, Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToRoundTripString()) }
        };

        if (exception is not null)
        {
            headers.Add(MessageHeaders.ExceptionType, Encoding.UTF8.GetBytes(exception.GetType().FullName ?? "Unknown"));
            headers.Add(MessageHeaders.ExceptionMessage, Encoding.UTF8.GetBytes(exception.Message));
        }

        var message = new Message<string, byte[]>
        {
            Key = envelope.MessageId,
            Value = envelope.Payload.ToArray(),
            Headers = headers
        };

        try
        {
            await producer.ProduceAsync(dlqTopic, message, cancellationToken);
            LogDeadLettered(logger, envelope.MessageId, dlqTopic, reason);
        }
        catch (Exception ex)
        {
            LogDeadLetterError(logger, ex, envelope.MessageId, dlqTopic);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Message {MessageId} sent to DLQ topic {Topic}: {Reason}")]
    static partial void LogDeadLettered(ILogger logger, string MessageId, string Topic, string Reason);

    [LoggerMessage(LogLevel.Error, "Failed to send message {MessageId} to DLQ topic {Topic}")]
    static partial void LogDeadLetterError(ILogger logger, Exception ex, string MessageId, string Topic);
}