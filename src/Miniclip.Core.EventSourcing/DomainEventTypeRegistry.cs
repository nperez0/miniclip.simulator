namespace Miniclip.Core.EventSourcing;

public sealed class DomainEventTypeRegistry(IReadOnlyDictionary<string, Type> types) : IDomainEventTypeRegistry
{
    public Type? Resolve(string typeName) => types.GetValueOrDefault(typeName);
}
