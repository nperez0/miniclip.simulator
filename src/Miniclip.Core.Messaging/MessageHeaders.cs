namespace Miniclip.Core.Messaging;

public static class MessageHeaders
{
    // --- Event identity (stamped by the write-side event bus) ---
    public const string ConcurrentId = "miniclip.concurrent-id";
    public const string EventId = "miniclip.event-id";
    public const string EventType = "miniclip.event-type";
    public const string OccurredOn = "miniclip.occurred-on";

    // --- Dead-letter metadata (stamped by any dead-letter handler) ---
    public const string OriginalConcurrentId = "miniclip.original-concurrent-id";
    public const string OriginalMessageId = "miniclip.original-message-id";
    public const string OriginalMessageType = "miniclip.original-message-type";
    public const string FailureReason = "miniclip.failure-reason";
    public const string FailedAt = "miniclip.failed-at";
    public const string ExceptionType = "miniclip.exception-type";
    public const string ExceptionMessage = "miniclip.exception-message";
}
