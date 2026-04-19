namespace Miniclip.Core.Messaging.Inbound;

public readonly record struct MessageHandlerResult
{
    public bool IsSuccess { get; }
    public bool ShouldRetry { get; }
    public string? ErrorMessage { get; }

    private MessageHandlerResult(bool isSuccess, bool shouldRetry, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ShouldRetry = shouldRetry;
        ErrorMessage = errorMessage;
    }

    public static MessageHandlerResult Success() => new(true, false, null);

    public static MessageHandlerResult TransientFailure(string error) => new(false, true, error);

    public static MessageHandlerResult PermanentFailure(string error) => new(false, false, error);
}
