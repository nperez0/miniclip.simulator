namespace Miniclip.Core.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrWhiteSpace(this string value)
        => string.IsNullOrWhiteSpace(value);

    public static void ThrowIfNullOrWhiteSpace(this string value, string name)
    {
        if (value.IsNullOrWhiteSpace())
            throw new ArgumentNullException(name);
    }
}
