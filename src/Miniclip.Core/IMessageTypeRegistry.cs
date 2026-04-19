namespace Miniclip.Core;

public interface IMessageTypeRegistry
{
    Type? Resolve(string typeName);
}
