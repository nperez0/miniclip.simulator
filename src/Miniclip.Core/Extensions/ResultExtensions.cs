namespace Miniclip.Core.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Chains a successful <see cref="Result{TIn}"/> into the next operation and short-circuits on failure.
    /// </summary>
    public static Result<TOut> Then<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> next)
        => result.IsFailure ? Result.Failure<TOut>(result.Exception) : next(result.Value!);

    /// <summary>
    /// Continues a successful non-generic <see cref="Result"/> flow with a value-producing operation.
    /// </summary>
    public static Result<T> Then<T>(this Result<T> result, Func<T, Result> next)
    {
        if (result.IsFailure)
            return Result.Failure<T>(result.Exception);

        var nextResult = next(result.Value!);
        return nextResult.IsFailure ? Result.Failure<T>(nextResult.Exception) : result;
    }

    /// <summary>
    /// Projects the success value of a <see cref="Result{TIn}"/> into a new <see cref="Result{TOut}"/>.
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map)
        => result.IsFailure ? Result.Failure<TOut>(result.Exception) : Result.Success(map(result.Value!));

    /// <summary>
    /// Projects a successful non-generic <see cref="Result"/> into a new typed result.
    /// </summary>
    public static Result<TOut> Map<TOut>(this Result result, Func<TOut> map)
        => result.IsFailure ? Result.Failure<TOut>(result.Exception) : Result.Success(map());

    /// <summary>
    /// Runs a side effect for a successful <see cref="Result{T}"/> without changing the result value.
    /// </summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> next)
    {
        if (result.IsFailure)
            return Result.Failure<T>(result.Exception);

        next(result.Value!);

        return result;
    }

    /// <summary>
    /// Runs a side effect for a successful non-generic <see cref="Result"/> without changing the result.
    /// </summary>
    public static Result Tap(this Result result, Action next)
    {
        if (result.IsFailure)
            return Result.Failure(result.Exception);

        next();

        return result;
    }

    /// <summary>
    /// Applies a result-producing operation to each item in a sequence and stops at the first failure.
    /// </summary>
    public static Result Traverse<T>(this IEnumerable<T> source, Func<T, Result> next)
    {
        foreach (var item in source)
        {
            var result = next(item);
            if (result.IsFailure)
                return result;
        }

        return Result.Success();
    }
}
