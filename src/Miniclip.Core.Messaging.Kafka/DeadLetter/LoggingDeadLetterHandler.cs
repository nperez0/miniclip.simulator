using Microsoft.Extensions.Logging;

namespace Miniclip.Core.Messaging.Kafka.DeadLetter;

internal sealed partial class LoggingDeadLetterHandler(ILogger<LoggingDeadLetterHandler> logger) : IDeadLetterHandler
{
    public Task HandleAsync(
        IMessageEnvelope envelope,
        string reason,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        LogPermanentlyFailed(logger, envelope.MessageId, envelope.MessageType, reason);
        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Warning,
        "Message {MessageId} of type {MessageType} permanently failed (reason: {Reason}). " +
        "No dead-letter producer is configured — message is dropped.")]
    static partial void LogPermanentlyFailed(
        ILogger logger, string MessageId, string MessageType, string Reason);
}
