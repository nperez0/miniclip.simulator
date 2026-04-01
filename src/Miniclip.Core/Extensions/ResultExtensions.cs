namespace Miniclip.Core.Extensions;

public static class ResultExtensions
{
    extension<TIn>(Result<TIn> result)
    {
        /// <summary>
        /// Chains a successful <see cref="Result{TIn}"/> into the next operation and short-circuits on failure.
        /// </summary>
        public Result<TOut> Then<TOut>(Func<TIn, Result<TOut>> next)
            => result.IsFailure ? Result.Failure<TOut>(result.Error) : next(result.Value!);

        /// <summary>
        /// Continues a successful non-generic <see cref="Result"/> flow with a value-producing operation.
        /// </summary>
        public Result<TIn> Then(Func<TIn, Result> next)
        {
            if (result.IsFailure)
                return Result.Failure<TIn>(result.Error);

            var nextResult = next(result.Value!);
            return nextResult.IsFailure ? Result.Failure<TIn>(nextResult.Error) : result;
        }

        /// <summary>
        /// Projects the success value of a <see cref="Result{TIn}"/> into a new <see cref="Result{TOut}"/>.
        /// </summary>
        public Result<TOut> Map<TOut>(Func<TIn, TOut> map)
            => result.IsFailure ? Result.Failure<TOut>(result.Error) : Result.Success(map(result.Value!));

        /// <summary>
        /// Runs a side effect for a successful <see cref="Result{T}"/> without changing the result value.
        /// </summary>
        public Result<TIn> Tap(Action<TIn> next)
        {
            if (result.IsFailure)
                return Result.Failure<TIn>(result.Error);

            next(result.Value!);

            return result;
        }
    }

    extension(Result result)
    {
        /// <summary>
        /// Projects a successful non-generic <see cref="Result"/> into a new typed result.
        /// </summary>
        public Result<TOut> Map<TOut>(Func<TOut> map)
            => result.IsFailure ? Result.Failure<TOut>(result.Error) : Result.Success(map());

        /// <summary>
        /// Runs a side effect for a successful non-generic <see cref="Result"/> without changing the result.
        /// </summary>
        public Result Tap(Action next)
        {
            if (result.IsFailure)
                return Result.Failure(result.Error);

            next();

            return result;
        }
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
