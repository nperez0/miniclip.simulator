using Miniclip.Core.Domain;
using Miniclip.Core.Messaging;

namespace Miniclip.Core.Application.IntegrationEvents;

public interface IIntegrationEventMapperRegistry
{
    IIntegrationEvent? TryMap(IDomainEvent domainEvent);
}
