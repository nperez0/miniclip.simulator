using Microsoft.Extensions.Logging;
using Miniclip.Core.Messaging.Inbound;

namespace Miniclip.Core.Messaging.Pipeline.Inbound.Middleware;

public sealed partial class RetryMiddleware(
    IRetryPolicy retryPolicy,
    ILogger<RetryMiddleware> logger) : IInboundMiddleware
{
    public async Task<MessageHandlerResult> InvokeAsync(
        IMessageEnvelope envelope,
        IMessageContext context,
        Func<Task<MessageHandlerResult>> next,
        CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (true)
        {
            attempt++;
            var result = await next();

            if (result.IsSuccess)
                return result;

            if (!result.ShouldRetry || attempt >= retryPolicy.MaxAttempts)
            {
                if (attempt >= retryPolicy.MaxAttempts)
                    LogExhausted(logger, envelope.MessageId, attempt);

                return MessageHandlerResult.PermanentFailure(
                    result.ErrorMessage ?? "Max retry attempts exceeded");
            }

            var delay = retryPolicy.GetDelay(attempt);
            LogRetrying(logger, envelope.MessageId, attempt, retryPolicy.MaxAttempts, delay);
            await Task.Delay(delay, cancellationToken);
        }
    }

    [LoggerMessage(LogLevel.Warning,
        "Message {MessageId}: Retry {Attempt}/{MaxAttempts}, waiting {Delay}ms")]
    static partial void LogRetrying(
        ILogger logger, string MessageId, int Attempt, int MaxAttempts, TimeSpan Delay);

    [LoggerMessage(LogLevel.Error,
        "Message {MessageId}: Exhausted retries after {Attempts} attempts")]
    static partial void LogExhausted(
        ILogger logger, string MessageId, int Attempts);
}
