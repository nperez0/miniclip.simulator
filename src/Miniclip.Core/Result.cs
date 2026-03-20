namespace Miniclip.Core;

public class Result(bool isSuccess, ExceptionBase exception)
{
    public bool IsSuccess { get; } = isSuccess;
    public bool IsFailure => !IsSuccess;
    public ExceptionBase Exception { get; } = exception;

    public static Result Success() => new(true, EmptyException.Instance);
    public static Result Failure(ExceptionBase exception) => new(false, exception);

    public static Result<T> Success<T>(T value) => new(value, true, EmptyException.Instance);
    public static Result<T> Failure<T>(ExceptionBase exception) => new(default, false, exception);
}

public class Result<T>(T? value, bool isSuccess, ExceptionBase exception) : Result(isSuccess, exception)
{
    public T? Value { get; } = value;

    public static implicit operator Result<T>(T value)
        => Success(value);
}
