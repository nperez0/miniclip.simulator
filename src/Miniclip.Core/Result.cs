namespace Miniclip.Core;

public class Result(bool isSuccess, Error error)
{
    public bool IsSuccess { get; } = isSuccess;
    public bool IsFailure => !IsSuccess;
    public Error Error { get; } = error;

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public class Result<T>(T? value, bool isSuccess, Error error) : Result(isSuccess, error)
{
    public T? Value { get; } = value;

    public static implicit operator Result<T>(T value)
        => Success(value);
}
