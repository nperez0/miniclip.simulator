using KurrentDB.Client.Extensions.OpenTelemetry;
using Miniclip.Core.Kafka;
using Miniclip.Core.ServiceDefaults.Extensions;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

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
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter()
                        .AddKafkaProducerInstrumentation<string, byte[]>()
                        .AddKafkaConsumerInstrumentation<string, byte[]>(TopicNaming.ForAggregate<Group>());
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter()
                        .AddKafkaProducerInstrumentation<string, byte[]>()
                        .AddKafkaConsumerInstrumentation<string, byte[]>(TopicNaming.ForAggregate<Group>())
                        .AddMySqlData()
                        .AddMySqlConnector()
                        .AddKurrentDBClientInstrumentation();
                });

            return services;
        }
    }
}
