namespace Miniclip.Core;

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Exception exception) => Result<T>.Failure(exception);
}

public class Result<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Exception Exception { get; }

    protected Result(T? value, bool isSuccess, Exception exception)
    {
        Value = value;
        IsSuccess = isSuccess;
        Exception = exception;
    }

    public static Result<T> Success(T value) => new(value, true, EmptyException.Instance);

    public static Result<T> Failure(Exception exception) => new(default, false, exception);
}
