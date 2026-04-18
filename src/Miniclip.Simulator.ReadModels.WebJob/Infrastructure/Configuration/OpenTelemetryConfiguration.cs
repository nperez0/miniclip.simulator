using Miniclip.Core.Messaging.Kafka;
using Miniclip.Core.OpenTelemetry.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

public static class OpenTelemetryConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpenTelemetryDependencies()
        {
            

            services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    foreach (var groupId in KafkaMessagingConfiguration.ConsumerGroupIds.Values)
                        metrics.AddKafkaConsumerInstrumentation<string, byte[]>(groupId);

                    metrics
                        .AddOtlpExporter()
                        .AddSimulator();
                })
                .WithTracing(tracing =>
                {
                    
                    foreach (var groupId in KafkaMessagingConfiguration.ConsumerGroupIds.Values)
                        tracing.AddKafkaConsumerInstrumentation<string, byte[]>(groupId);

                    tracing
                        .AddOtlpExporter()
                        .AddMySqlData()
                        .AddMySqlConnector()
                        .AddSimulator();
                });

            return services;
        }
    }
}
