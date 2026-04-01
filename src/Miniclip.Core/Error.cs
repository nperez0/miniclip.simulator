namespace Miniclip.Core;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    General
}

public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.General)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string code, string message)
        => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message)
        => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message)
        => new(code, message, ErrorType.Conflict);
}
