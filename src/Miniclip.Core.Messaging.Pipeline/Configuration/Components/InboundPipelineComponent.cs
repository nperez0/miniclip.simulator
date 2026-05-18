using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miniclip.Core.Messaging.Configuration;
using Miniclip.Core.Messaging.Pipeline.Inbound;

namespace Miniclip.Core.Messaging.Pipeline.Configuration.Components;

public sealed class InboundPipelineComponent(
    InboundPipelineOptions options,
    IReadOnlyList<Type> middlewareChain) : IMessagingComponent
{
    public void Register(IServiceCollection services)
    {
        foreach (var middlewareType in middlewareChain)
            services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IInboundMiddleware), middlewareType));

        services.TryAddSingleton(options.RetryPolicy);
        services.TryAddSingleton<IInboundPipeline, MessagePipeline>();
    }
}
