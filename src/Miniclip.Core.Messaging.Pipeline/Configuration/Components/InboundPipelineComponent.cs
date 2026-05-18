using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miniclip.Core.Messaging.Configuration;
using Miniclip.Core.Messaging.Pipeline.Inbound;

namespace Miniclip.Core.Messaging.Pipeline.Configuration.Components;

public sealed class InboundPipelineComponent(
    InboundPipelineOptions options,
    Type[] middlewareChain) : IMessagingComponent
{
    public void Register(IServiceCollection services)
    {
        foreach (var middlewareType in middlewareChain)
            services.TryAddScoped(middlewareType);

        services.TryAddSingleton(options.RetryPolicy);
        services.TryAddSingleton<IInboundPipeline>(sp =>
            new MessagePipeline(
                middlewareChain,
                sp.GetRequiredService<IMessageHandlerRegistry>(),
                sp.GetRequiredService<IMessageDeserializer>(),
                sp.GetRequiredService<IServiceScopeFactory>()));
    }
}
