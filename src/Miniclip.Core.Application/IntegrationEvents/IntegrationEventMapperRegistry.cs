using System.Collections.Frozen;

namespace Miniclip.Core.Application.IntegrationEvents;

internal sealed class IntegrationEventMapperRegistry : IIntegrationEventMapperRegistry
{
    private readonly FrozenDictionary<Type, Func<IDomainEvent, IIntegrationEvent>> mappers;
    private readonly IReadOnlyCollection<string> messageTypeNames;

    internal IntegrationEventMapperRegistry(
        IEnumerable<(Type domainEventType, string integrationEventMessageTypeName, Func<IDomainEvent, IIntegrationEvent> map)> entries)
    {
        var materialized = entries.ToList();
        mappers = materialized.ToFrozenDictionary(e => e.domainEventType, e => e.map);
        messageTypeNames = materialized.Select(e => e.integrationEventMessageTypeName).ToArray();
    }

    public IIntegrationEvent? TryMap(IDomainEvent domainEvent) =>
        mappers.TryGetValue(domainEvent.GetType(), out var map) ? map(domainEvent) : null;
}