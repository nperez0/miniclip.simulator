namespace Miniclip.Core.Application.IntegrationEvents;

public interface IIntegrationEventMapper<in TDomainEvent, out TIntegrationEvent>
    where TDomainEvent : IDomainEvent
    where TIntegrationEvent : IIntegrationEvent
{
    TIntegrationEvent Map(TDomainEvent domainEvent);
}
