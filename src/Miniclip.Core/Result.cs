namespace Miniclip.Core;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Exception Exception { get; }

    protected Result(bool isSuccess, Exception exception)
    {
        IsSuccess = isSuccess;
        Exception = exception;
    }

    public static Result Success() => new(true, EmptyException.Instance);
    public static Result Failure(Exception exception) => new(false, exception);

    public static Result<T> Success<T>(T value) => new(value, true, EmptyException.Instance);
    public static Result<T> Failure<T>(Exception exception) => new(default, false, exception);
}

public class Result<T> : Result
{
    public T? Value { get; }

    public Result(T? value, bool isSuccess, Exception exception)
        : base(isSuccess, exception)
    {
        Value = value;
    }

    public static implicit operator Result<T>(T value)
        => Result.Success(value);
}
