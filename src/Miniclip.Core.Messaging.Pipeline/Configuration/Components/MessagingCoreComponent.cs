using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging.Configuration;
using Miniclip.Core.Propagation.Configuration;

namespace Miniclip.Core.Messaging.Pipeline.Configuration.Components;

public sealed class MessagingCoreComponent : IMessagingComponent
{
    public void Register(IServiceCollection services)
    {
        services.AddPropagationContext();
    }
}
