using System.Diagnostics.CodeAnalysis;

namespace Miniclip.Core.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
        => string.IsNullOrWhiteSpace(value);
}
