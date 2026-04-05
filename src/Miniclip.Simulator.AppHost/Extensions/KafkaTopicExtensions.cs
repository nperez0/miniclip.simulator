using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace Miniclip.Simulator.AppHost.Extensions;

internal sealed class KafkaTopicsResource(string name) : Resource(name), IResourceWithWaitSupport;

internal sealed record KafkaTopicsAnnotation(string[] Topics, KafkaServerResource KafkaResource) : IResourceAnnotation;

internal static class KafkaTopicExtensions
{
    private static readonly string[] topics = ["simulator.group"];

    internal static IResourceBuilder<KafkaTopicsResource> WithTopicCreation(
        this IResourceBuilder<KafkaServerResource> kafka)
    {
        var resource = new KafkaTopicsResource($"{kafka.Resource.Name}-topics");

        kafka.ApplicationBuilder.Services.TryAddEventingSubscriber<KafkaTopicsEventingSubscriber>();

        return kafka.ApplicationBuilder
            .AddResource(resource)
            .WaitFor(kafka)
            .WithAnnotation(new KafkaTopicsAnnotation(topics, kafka.Resource));
    }
}

internal sealed partial class KafkaTopicsEventingSubscriber(
    DistributedApplicationModel appModel,
    ResourceNotificationService notifications,
    ResourceLoggerService loggers) : IDistributedApplicationEventingSubscriber
{
    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        eventing.Subscribe<BeforeStartEvent>(async (_, ct) =>
        {
            foreach (var resource in appModel.Resources.OfType<KafkaTopicsResource>())
                await notifications.PublishUpdateAsync(resource, s => s with
                {
                    State = new ResourceStateSnapshot(KnownResourceStates.Starting, KnownResourceStateStyles.Info)
                });
        });

        foreach (var resource in appModel.Resources.OfType<KafkaTopicsResource>())
        {
            var annotation = resource.Annotations.OfType<KafkaTopicsAnnotation>().Single();
            eventing.Subscribe<ResourceReadyEvent>(annotation.KafkaResource, async (_, ct) =>
                await CreateTopicsAsync(resource, ct));
        }

        return Task.CompletedTask;
    }

    private async Task CreateTopicsAsync(KafkaTopicsResource resource, CancellationToken cancellationToken)
    {
        var annotation = resource.Annotations.OfType<KafkaTopicsAnnotation>().Single();
        var logger = loggers.GetLogger(resource);

        try
        {
            var connectionString = await ((IResourceWithConnectionString)annotation.KafkaResource)
                .GetConnectionStringAsync(cancellationToken)
                ?? throw new InvalidOperationException($"No connection string available for Kafka resource '{annotation.KafkaResource.Name}'.");

            using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = connectionString }).Build();

            var specs = annotation.Topics
                .Select(t => new TopicSpecification { Name = t, NumPartitions = 1, ReplicationFactor = 1 })
                .ToList();

            try
            {
                await adminClient.CreateTopicsAsync(specs);
                LogCreatedKafkaTopics(logger, string.Join(", ", annotation.Topics));
            }
            catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                LogKafkaTopicsAlreadyExist(logger, string.Join(", ", annotation.Topics));
            }

            await notifications.PublishUpdateAsync(resource, s => s with
            {
                State = new ResourceStateSnapshot(KnownResourceStates.Running, KnownResourceStateStyles.Success)
            });
        }
        catch
        {
            LogFailedToCreateKafkaTopicsForResource(logger, resource.Name);

            await notifications.PublishUpdateAsync(resource, s => s with
            {
                State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error)
            });
        }
    }

    [LoggerMessage(LogLevel.Information, "Created Kafka topics: {Topics}")]
    static partial void LogCreatedKafkaTopics(ILogger logger, string Topics);

    [LoggerMessage(LogLevel.Information, "Kafka topics already exist: {Topics}")]
    static partial void LogKafkaTopicsAlreadyExist(ILogger logger, string Topics);

    [LoggerMessage(LogLevel.Error, "Failed to create Kafka topics for '{Resource}'")]
    static partial void LogFailedToCreateKafkaTopicsForResource(ILogger logger, string Resource);
}
