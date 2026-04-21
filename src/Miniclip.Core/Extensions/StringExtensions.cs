using System.Diagnostics.CodeAnalysis;

namespace Miniclip.Core.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
        => string.IsNullOrEmpty(value);

    public static bool IsNotNullOrEmpty([NotNullWhen(true)] this string? value)
        => string.IsNullOrEmpty(value);

    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
        => string.IsNullOrWhiteSpace(value);

    public static bool IsNotNullOrWhiteSpace([NotNullWhen(true)] this string? value)
        => string.IsNullOrWhiteSpace(value);
}
