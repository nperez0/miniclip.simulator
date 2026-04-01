namespace Miniclip.Core;

public enum ErrorType
{
    Conflict,
    Validation,
    NotFound
}

public sealed record Error(string Code, string[] Messages, ErrorType Type = ErrorType.Conflict)
{
    public static readonly Error None = new(string.Empty, []);

    public static Error Validation(string code, string[] message)
        => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message)
        => new(code, [message], ErrorType.NotFound);

    public static Error Conflict(string code, string message)
        => new(code, [message]);
}
