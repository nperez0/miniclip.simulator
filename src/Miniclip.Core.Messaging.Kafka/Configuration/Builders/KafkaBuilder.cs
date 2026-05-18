using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging.Configuration;
using Miniclip.Core.Messaging.Kafka.Configuration.Components;
using Miniclip.Core.Messaging.Pipeline.Configuration.Components;

namespace Miniclip.Core.Messaging.Kafka.Configuration.Builders;

public sealed class KafkaBuilder
{
    private readonly IServiceCollection services;
    private readonly string bootstrapServers;
    private readonly List<IMessagingComponent> extraComponents = [];

    private IMessagingComponent typeRegistrySlot = new MessageTypeRegistryComponent();
    private InboundKafkaBuilder? inbound;
    private OutboundKafkaBuilder? outbound;

    internal KafkaBuilder(IServiceCollection services, string bootstrapServers)
    {
        this.services = services;
        this.bootstrapServers = bootstrapServers;
    }

    public KafkaBuilder ConfigureInbound(Action<InboundKafkaBuilder> configure)
    {
        if (inbound is not null)
            throw new InvalidOperationException("ConfigureInbound has already been called. It can only be configured once.");

        inbound = new InboundKafkaBuilder(bootstrapServers);
        configure(inbound);
        return this;
    }

    public KafkaBuilder ConfigureOutbound(Action<OutboundKafkaBuilder> configure)
    {
        if (outbound is not null)
            throw new InvalidOperationException("ConfigureOutbound has already been called. It can only be configured once.");

        outbound = new OutboundKafkaBuilder(bootstrapServers);
        configure(outbound);
        return this;
    }

    public KafkaBuilder UseTypeRegistry(IMessagingComponent component)
    {
        typeRegistrySlot = component;
        return this;
    }

    public KafkaBuilder AddComponent(IMessagingComponent component)
    {
        extraComponents.Add(component);
        return this;
    }

    internal void Build()
    {
        var components = new List<IMessagingComponent>
        {
            new MessagingCoreComponent(),
            typeRegistrySlot,
        };

        if (outbound is not null)
            components.AddRange(outbound.BuildComponents());

        if (inbound is not null)
            components.AddRange(inbound.BuildComponents());

        components.AddRange(extraComponents);

        foreach (var component in components)
            component.Register(services);
    }
}
