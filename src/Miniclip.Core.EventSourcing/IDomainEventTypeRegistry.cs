namespace Miniclip.Core.EventSourcing;

public interface IDomainEventTypeRegistry
{
    Type? Resolve(string typeName);
}
