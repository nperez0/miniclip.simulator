using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miniclip.Core.Messaging.Configuration;

namespace Miniclip.Core.Messaging.Pipeline.Configuration.Components;

public sealed class MessagingCoreComponent : IMessagingComponent
{
    public void Register(IServiceCollection services)
    {
        services.TryAddScoped<PropagationContext>();
        services.TryAddScoped<IPropagationContext>(sp => sp.GetRequiredService<PropagationContext>());
        services.TryAddScoped<IMutablePropagationContext>(sp => sp.GetRequiredService<PropagationContext>());
    }
}
