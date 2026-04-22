
namespace Miniclip.Core.Application.IntegrationEvents;

public interface IIntegrationEventMapperRegistry
{
    IIntegrationEvent? TryMap(IDomainEvent domainEvent);
}
