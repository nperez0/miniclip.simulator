using Microsoft.Extensions.DependencyInjection;

namespace Miniclip.Core.Messaging.Configuration;

public interface IMessagingComponent
{
    void Register(IServiceCollection services);
}
