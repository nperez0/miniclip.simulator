using Miniclip.Core.Domain;
using Miniclip.Core.Messaging;

namespace Miniclip.Core.Application.IntegrationEvents;

public interface IIntegrationEventMapper<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    IIntegrationEvent Map(TDomainEvent domainEvent);
}
