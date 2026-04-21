using System.Collections.Frozen;
using Miniclip.Core.Domain;
using Miniclip.Core.Messaging;

namespace Miniclip.Core.Application.IntegrationEvents;

internal sealed class IntegrationEventMapperRegistry : IIntegrationEventMapperRegistry
{
    private readonly FrozenDictionary<Type, Func<IDomainEvent, IIntegrationEvent>> mappers;

    internal IntegrationEventMapperRegistry(IEnumerable<(Type domainEventType, Func<IDomainEvent, IIntegrationEvent> map)> entries)
    {
        mappers = entries.ToFrozenDictionary(e => e.domainEventType, e => e.map);
    }

    public IIntegrationEvent? TryMap(IDomainEvent domainEvent)
    {
        return mappers.TryGetValue(domainEvent.GetType(), out var map)
            ? map(domainEvent)
            : null;
    }
}
