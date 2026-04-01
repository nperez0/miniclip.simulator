namespace Miniclip.Core;

public sealed class Validation<T>
{
    private readonly List<string> messages = [];

    private readonly string code = $"{typeof(T).Name.ToUpper()}_VALIDATION_FAILED";

    public Validation<T> Ensure(bool condition, string message)
    {
        if (!condition) messages.Add(message);
        return this;
    }

    public Result Validate()
        => messages.Count == 0
            ? Result.Success()
            : Result.Failure(Error.Validation(code, messages.ToArray()));
}

public static class Validation
{
    public static Validation<T> For<T>() => new();
}
