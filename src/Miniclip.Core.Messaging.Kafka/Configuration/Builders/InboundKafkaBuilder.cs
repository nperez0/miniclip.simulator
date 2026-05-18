using Miniclip.Core.Messaging.Configuration;
using Miniclip.Core.Messaging.Kafka.Configuration.Components;
using Miniclip.Core.Messaging.Kafka.Configuration.Components.DeadLetter;
using Miniclip.Core.Messaging.Pipeline.Configuration;
using Miniclip.Core.Messaging.Pipeline.Configuration.Builders;
using Miniclip.Core.Messaging.Pipeline.Configuration.Components;
using Miniclip.Core.Messaging.Pipeline.Inbound.Middleware;

namespace Miniclip.Core.Messaging.Kafka.Configuration.Builders;

public sealed class InboundKafkaBuilder
{
    private readonly string bootstrapServers;
    private readonly List<KafkaConsumerDescriptor> consumers = [];

    private readonly InboundPipelineOptions pipelineOptions = new();
    private IMessagingComponent deadLetterSlot = new LoggingDeadLetterComponent();
    private IMessagingComponent handlerScanningSlot = new MessageHandlersComponent();
    private IMessagingComponent deserializerSlot = new JsonDeserializerComponent();

    private readonly MiddlewareBuilder<IInboundMiddleware> middlewareChain =
        new MiddlewareBuilder<IInboundMiddleware>()
            .Add<TracingMiddleware>()
            .Add<LoggingMiddleware>()
            .Add<RetryMiddleware>();

    internal InboundKafkaBuilder(string bootstrapServers)
    {
        this.bootstrapServers = bootstrapServers;
    }

    public InboundKafkaBuilder ConfigureMiddleware(Action<MiddlewareBuilder<IInboundMiddleware>> configure)
    {
        configure(middlewareChain);
        return this;
    }

    public InboundKafkaBuilder UseRetryPolicy(IRetryPolicy policy)
    {
        pipelineOptions.UseRetryPolicy(policy);
        return this;
    }

    public InboundKafkaBuilder UseDeserializer(IMessagingComponent component)
    {
        deserializerSlot = component;
        return this;
    }

    public InboundKafkaBuilder UseJsonDeserializer()
    {
        deserializerSlot = new JsonDeserializerComponent();
        return this;
    }

    public InboundKafkaBuilder UseHandlerScanning(IMessagingComponent component)
    {
        handlerScanningSlot = component;
        return this;
    }

    public InboundKafkaBuilder UseDeadLetter(IMessagingComponent component)
    {
        deadLetterSlot = component;
        return this;
    }

    public InboundKafkaBuilder UseKafkaDeadLetter()
    {
        deadLetterSlot = new KafkaDeadLetterComponent(bootstrapServers);
        return this;
    }

    public InboundKafkaBuilder AddConsumer(Action<KafkaConsumerSubscriptionBuilder> configure)
    {
        var builder = new KafkaConsumerSubscriptionBuilder();
        configure(builder);
        consumers.Add(builder.Build());
        return this;
    }

    internal IReadOnlyList<IMessagingComponent> BuildComponents()
    {
        var chain = middlewareChain.Build();

        var components = new List<IMessagingComponent>
        {
            new InboundPipelineComponent(pipelineOptions, chain.ToArray()),
            deserializerSlot,
            handlerScanningSlot,
            deadLetterSlot,
        };

        var kafkaInboundConsumerComponents = consumers
            .Select(descriptor => new KafkaInboundConsumerComponent(bootstrapServers, descriptor));

        components.AddRange(kafkaInboundConsumerComponents);

        return components;
    }
}
