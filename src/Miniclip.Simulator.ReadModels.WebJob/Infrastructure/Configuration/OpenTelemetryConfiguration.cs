using Miniclip.Core.Kafka;
using Miniclip.Core.OpenTelemetry.Extensions;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
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
                    metrics
                        .AddOtlpExporter()
                        .AddKafkaConsumerInstrumentation<string, byte[]>(TopicNaming.ForAggregate<Group>())
                        .AddSimulator();
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddOtlpExporter()
                        .AddKafkaConsumerInstrumentation<string, byte[]>(TopicNaming.ForAggregate<Group>())
                        .AddMySqlData()
                        .AddMySqlConnector()
                        .AddSimulator();
                });

            return services;
        }
    }
}
