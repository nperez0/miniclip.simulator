namespace Miniclip.Core;

public class EmptyException : Exception
{
    public static readonly EmptyException Instance = new();
}
