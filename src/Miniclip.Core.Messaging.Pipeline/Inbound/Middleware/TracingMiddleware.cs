using System.Diagnostics;
using Miniclip.Core.Extensions;
using Miniclip.Core.OpenTelemetry;

namespace Miniclip.Core.Messaging.Pipeline.Inbound.Middleware;

public sealed class TracingMiddleware : IInboundMiddleware
{
    public async Task<MessageHandlerResult> InvokeAsync(
        IMessageEnvelope envelope,
        IMessageContext context,
        Func<Task<MessageHandlerResult>> next,
        CancellationToken cancellationToken)
    {
        var parentContext = ExtractParentContext(context);

        using var activity = OpenTelemetryActivity.StartActivity(
            $"{OpenTelemetryConstants.Tags.MessageProcess} {envelope.MessageType}",
            ActivityKind.Consumer,
            parentContext: parentContext);

        activity.SetTag(OpenTelemetryConstants.Tags.MessageId, envelope.MessageId);
        activity.SetTag(OpenTelemetryConstants.Tags.MessageType, envelope.MessageType);
        activity.SetTag(OpenTelemetryConstants.Tags.SubscriptionId, context.SubscriptionId);

        SetTagFromMessageContext(activity, context, MessageHeaders.CorrelationId, OpenTelemetryConstants.Tags.CorrelationId);
        SetTagFromMessageContext(activity, context, MessageHeaders.CausationId, OpenTelemetryConstants.Tags.CausationId);
        SetTagFromMessageContext(activity, context, MessageHeaders.EventId, OpenTelemetryConstants.Tags.EventId);
        SetTagFromMessageContext(activity, context, MessageHeaders.AggregateId, OpenTelemetryConstants.Tags.AggregateId);
        SetTagFromMessageContext(activity, context, MessageHeaders.AggregateType, OpenTelemetryConstants.Tags.AggregateType);
        SetTagFromMessageContext(activity, context, MessageHeaders.AggregateVersion, OpenTelemetryConstants.Tags.AggregateVersion);

        var result = await next();

        if (result.IsSuccess)
            return result;

        var exception = new InvalidOperationException(result.ErrorMessage ?? "Handler failed");
        activity.NoticeError(exception);

        return result;
    }

    private static ActivityContext? ExtractParentContext(IMessageContext context)
    {
        var traceParent = context.Headers.GetValueOrDefault(MessageHeaders.TraceParent);

        if (traceParent.IsNullOrEmpty())
            return null;

        var traceState = context.Headers.GetValueOrDefault(MessageHeaders.TraceState);

        return ActivityContext.TryParse(traceParent, traceState, isRemote: true, out var ctx)
            ? ctx
            : null;
    }

    private static void SetTagFromMessageContext(
        OpenTelemetryActivity activity, 
        IMessageContext context, 
        string headerKey,
        string tagKey)
    {
        var value = context.Headers.GetValueOrDefault(headerKey);
        if (value.IsNotNullOrEmpty())
            activity.SetTag(tagKey, value);
    }
}
