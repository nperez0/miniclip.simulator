namespace Miniclip.Core;

public class ExceptionBase : Exception
{
    public ExceptionType Type { get; }

    public ExceptionBase(ExceptionType type = ExceptionType.General)
    {
        Type = type;
    }

    public ExceptionBase(string message, ExceptionType type = ExceptionType.General)
        : base(message)
    {
        Type = type;
    }
}
