namespace Miniclip.Core.Extensions;

public static class IEnumerableExtensions
{
    public static void Each<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach(var item in source)
            action(item);
    }
}
