using System.Diagnostics;
using Miniclip.Core.OpenTelemetry;

namespace Miniclip.Core.Messaging.Pipeline.Outbound.Middleware;

public sealed class OutboundTracingMiddleware : IOutboundMiddleware
{
    public async Task InvokeAsync(OutboundEnvelope envelope, Func<Task> next, CancellationToken cancellationToken)
    {
        var eventType = envelope.Event.GetType().Name;

        using var activity = OpenTelemetryActivity.StartActivity(
            $"{OpenTelemetryConstants.Tags.MessagePublish} {eventType}",
            ActivityKind.Producer);

        SetTag(activity, envelope.Headers, MessageHeaders.MessageId, OpenTelemetryConstants.Tags.MessageId);
        SetTag(activity, envelope.Headers, MessageHeaders.EventId, OpenTelemetryConstants.Tags.EventId);
        SetTag(activity, envelope.Headers, MessageHeaders.AggregateId, OpenTelemetryConstants.Tags.AggregateId);
        SetTag(activity, envelope.Headers, MessageHeaders.AggregateType, OpenTelemetryConstants.Tags.AggregateType);
        SetTag(activity, envelope.Headers, MessageHeaders.AggregateVersion, OpenTelemetryConstants.Tags.AggregateVersion);
        SetTag(activity, envelope.Headers, MessageHeaders.CorrelationId, OpenTelemetryConstants.Tags.CorrelationId);
        SetTag(activity, envelope.Headers, MessageHeaders.CausationId, OpenTelemetryConstants.Tags.CausationId);

        await next();
    }

    private static void SetTag(
        OpenTelemetryActivity activity,
        Dictionary<string, string> headers,
        string headerKey,
        string tagKey)
    {
        if (headers.TryGetValue(headerKey, out var value))
            activity.SetTag(tagKey, value);
    }
}
