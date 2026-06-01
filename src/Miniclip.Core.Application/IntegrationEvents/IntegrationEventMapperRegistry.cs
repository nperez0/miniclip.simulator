using System.Collections.Frozen;

namespace Miniclip.Core.Application.IntegrationEvents;

internal sealed class IntegrationEventMapperRegistry : IIntegrationEventMapperRegistry
{
    private readonly FrozenDictionary<Type, Func<IDomainEvent, IIntegrationEvent>> mappers;

    public IReadOnlyCollection<string> MappedDomainEventTypeNames { get; }

    internal IntegrationEventMapperRegistry(
        IEnumerable<(Type domainEventType, string integrationEventMessageTypeName, Func<IDomainEvent, IIntegrationEvent> map)> entries)
    {
        var materialized = entries.ToList();
        mappers = materialized.ToFrozenDictionary(e => e.domainEventType, e => e.map);
        MappedDomainEventTypeNames = materialized.Select(e => e.domainEventType.Name).ToArray();
    }

    public IIntegrationEvent? TryMap(IDomainEvent domainEvent) =>
        mappers.TryGetValue(domainEvent.GetType(), out var map) ? map(domainEvent) : null;
}