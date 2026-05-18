using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miniclip.Core.Messaging.Configuration;

namespace Miniclip.Core.Messaging.Pipeline.Configuration.Components;

public sealed class OutboundPipelineComponent(IReadOnlyList<Type> middlewareChain) : IMessagingComponent
{
    public void Register(IServiceCollection services)
    {
        foreach (var middlewareType in middlewareChain)
            services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IOutboundMiddleware), middlewareType));
    }
}
