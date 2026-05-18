namespace Miniclip.Core.Messaging.Pipeline.Configuration.Builders;

public sealed class MiddlewareBuilder<TMiddleware> where TMiddleware : class
{
    private readonly LinkedList<Type> entries = [];

    public MiddlewareBuilder<TMiddleware> Add<T>() where T : class, TMiddleware
    {
        if (entries.Contains(typeof(T)))
            throw new InvalidOperationException(
                $"Middleware '{typeof(T).Name}' is already registered in the chain.");

        entries.AddLast(typeof(T));
        return this;
    }

    public MiddlewareBuilder<TMiddleware> InsertBefore<TBefore, T>()
        where TBefore : class, TMiddleware
        where T : class, TMiddleware
    {
        if (entries.Contains(typeof(T)))
            throw new InvalidOperationException(
                $"Middleware '{typeof(T).Name}' is already registered in the chain.");

        var node = entries.Find(typeof(TBefore));
        if (node is null)
            return this;

        entries.AddBefore(node, typeof(T));
        return this;
    }

    public MiddlewareBuilder<TMiddleware> InsertAfter<TAfter, T>()
        where TAfter : class, TMiddleware
        where T : class, TMiddleware
    {
        if (entries.Contains(typeof(T)))
            throw new InvalidOperationException(
                $"Middleware '{typeof(T).Name}' is already registered in the chain.");

        var node = entries.Find(typeof(TAfter));
        if (node is null)
            return this;

        entries.AddAfter(node, typeof(T));
        return this;
    }

    public MiddlewareBuilder<TMiddleware> Remove<T>() where T : class, TMiddleware
    {
        entries.Remove(typeof(T));
        return this;
    }

    public MiddlewareBuilder<TMiddleware> Replace<TOld, TNew>()
        where TOld : class, TMiddleware
        where TNew : class, TMiddleware
    {
        var node = entries.Find(typeof(TOld));
        if (node is null)
            return this;

        entries.AddAfter(node, typeof(TNew));
        entries.Remove(node);
        return this;
    }

    public IReadOnlyList<Type> Build() => [.. entries];
}
