using Miniclip.Core.OpenTelemetry;

namespace Miniclip.Core.Messaging.Pipeline.Middleware;

public sealed class TracingMiddleware : IMessageMiddleware
{
    public async Task<MessageHandlerResult> InvokeAsync(
        IMessageEnvelope envelope,
        IMessageContext context,
        Func<Task<MessageHandlerResult>> next,
        CancellationToken cancellationToken)
    {
        using var activity = OpenTelemetryActivity.StartActivity(
            $"{OpenTelemetryConstants.Tags.MessageProcess} {envelope.MessageType}");

        activity.SetTag(OpenTelemetryConstants.Tags.MessageId, envelope.MessageId);
        activity.SetTag(OpenTelemetryConstants.Tags.MessageType, envelope.MessageType);
        activity.SetTag(OpenTelemetryConstants.Tags.SubscriptionId, context.SubscriptionId);

        if (context.Headers.TryGetValue(MessageHeaders.ConcurrentId, out var correlationId))
            activity.SetTag(OpenTelemetryConstants.Tags.CorrelationId, correlationId);

        var result = await next();

        if (result.IsSuccess) 
            return result;

        var exception = new InvalidOperationException(result.ErrorMessage ?? "Handler failed");
        activity.NoticeError(exception);

        return result;
    }
}
