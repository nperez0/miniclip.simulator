using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Messaging;
using Miniclip.Core.Reflection;

namespace Miniclip.Core.Messaging.Kafka.Configuration;

public sealed class KafkaBuilder
{
    private readonly IServiceCollection services;
    private readonly string bootstrapServers;
    private InboundKafkaBuilder? inbound;
    private OutboundKafkaBuilder? outbound;

    internal KafkaBuilder(IServiceCollection services, string bootstrapServers)
    {
        this.services = services;
        this.bootstrapServers = bootstrapServers;
    }

    public KafkaBuilder ConfigureInbound(Action<InboundKafkaBuilder> configure)
    {
        inbound = new InboundKafkaBuilder();
        configure(inbound);
        return this;
    }

    public KafkaBuilder ConfigureOutbound(Action<OutboundKafkaBuilder> configure)
    {
        outbound = new OutboundKafkaBuilder();
        configure(outbound);
        return this;
    }

    internal void Build()
    {
        RegisterSerializer();
        outbound?.Build(services, bootstrapServers);
        inbound?.Build(services, bootstrapServers);
    }

    private void RegisterSerializer()
    {
        var types = AssemblyScanner
            .GetImplementationsOf<IIntegrationEvent>()
            .ToDictionary(
                t => t.GetMessageTypeName(),
                StringComparer.Ordinal);

        services.AddSingleton<IMessageTypeRegistry>(new MessageTypeRegistry(types));
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
    }
}
