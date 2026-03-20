namespace Miniclip.Core;

public class EmptyException : ExceptionBase
{
    public static readonly EmptyException Instance = new();
}
