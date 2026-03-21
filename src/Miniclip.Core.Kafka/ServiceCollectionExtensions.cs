using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Miniclip.Core.Application;

namespace Miniclip.Core.Kafka;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaEventBus(
        this IServiceCollection services,
        string bootstrapServers)
    {
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };

        services.AddSingleton<IProducer<string, byte[]>>(_ =>
            new ProducerBuilder<string, byte[]>(config).Build());

        services.AddSingleton<IEventBus, KafkaEventBus>();

        return services;
    }
}
