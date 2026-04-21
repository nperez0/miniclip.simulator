namespace Miniclip.Core.Messaging;

public interface IMessageTypeRegistry
{
    Type? Resolve(string typeName);
}
