using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Messaging.Inbound;

namespace Miniclip.Core.Messaging.Pipeline.Inbound.Middleware;

public sealed partial class LoggingMiddleware(
    ILogger<LoggingMiddleware> logger) : IInboundMiddleware
{
    public async Task<MessageHandlerResult> InvokeAsync(
        IMessageEnvelope envelope,
        IMessageContext context,
        Func<Task<MessageHandlerResult>> next,
        CancellationToken cancellationToken)
    {
        LogStarting(logger, envelope.MessageId, envelope.MessageType, context.SubscriptionId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();
            stopwatch.Stop();

            if (result.IsSuccess)
                LogCompleted(logger, envelope.MessageId, stopwatch.ElapsedMilliseconds);
            else
                LogFailed(logger, envelope.MessageId, result.ErrorMessage ?? "Unknown", stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogException(logger, ex, envelope.MessageId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    [LoggerMessage(LogLevel.Information,
        "Processing message {MessageId} ({MessageType}) for subscription {SubscriptionId}")]
    static partial void LogStarting(
        ILogger logger, string MessageId, string MessageType, string SubscriptionId);

    [LoggerMessage(LogLevel.Information,
        "Message {MessageId} processed successfully in {ElapsedMs}ms")]
    static partial void LogCompleted(
        ILogger logger, string MessageId, long ElapsedMs);

    [LoggerMessage(LogLevel.Warning,
        "Message {MessageId} handler failed: {Error} ({ElapsedMs}ms)")]
    static partial void LogFailed(
        ILogger logger, string MessageId, string Error, long ElapsedMs);

    [LoggerMessage(LogLevel.Error,
        "Message {MessageId} handler threw exception ({ElapsedMs}ms)")]
    static partial void LogException(
        ILogger logger, Exception ex, string MessageId, long ElapsedMs);
}
