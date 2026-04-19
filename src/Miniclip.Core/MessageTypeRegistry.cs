namespace Miniclip.Core;

public sealed class MessageTypeRegistry(IReadOnlyDictionary<string, Type> types) : IMessageTypeRegistry
{
    public Type? Resolve(string typeName) => types.GetValueOrDefault(typeName);
}
