namespace Miniclip.Core.Messaging;

public static class MessageHeaders
{
    public const string MessageId = "miniclip.message-id";
    public const string EventId = "miniclip.event-id";
    public const string EventType = "miniclip.event-type";
    public const string OccurredOn = "miniclip.occurred-on";
    public const string CorrelationId = "miniclip.correlation-id";
    public const string CausationId = "miniclip.causation-id";
    public const string AggregateId = "miniclip.aggregate-id";
    public const string AggregateType = "miniclip.aggregate-type";
    public const string AggregateVersion = "miniclip.aggregate-version";
    public const string OriginalCorrelationId = "miniclip.original-correlation-id";
    public const string OriginalMessageId = "miniclip.original-message-id";
    public const string OriginalMessageType = "miniclip.original-message-type";
    public const string FailureReason = "miniclip.failure-reason";
    public const string FailedAt = "miniclip.failed-at";
    public const string ExceptionType = "miniclip.exception-type";
    public const string ExceptionMessage = "miniclip.exception-message";

    // W3C TraceContext propagation
    public const string TraceParent = "traceparent";
    public const string TraceState  = "tracestate";
}
