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
            services
                .AddOpenTelemetry()
                .WithMetrics(metrics => metrics
                    .AddOtlpExporter()
                    .AddSimulator())
                .WithTracing(tracing => tracing
                    .AddOtlpExporter()
                    .AddMySqlData()
                    .AddMySqlConnector()
                    .AddSimulator());

            services.ConfigureOpenTelemetryMeterProvider((sp, metrics) =>
            {
                metrics.AddKafkaProducerInstrumentation<string, byte[]>();

                foreach (var group in sp.GetServices<ConsumerGroup>())
                    metrics.AddKafkaConsumerInstrumentation<string, byte[]>(group.Id);
            });

            services.ConfigureOpenTelemetryTracerProvider((sp, tracing) =>
            {
                tracing.AddKafkaProducerInstrumentation<string, byte[]>();

                foreach (var group in sp.GetServices<ConsumerGroup>())
                    tracing.AddKafkaConsumerInstrumentation<string, byte[]>(group.Id);
            });

            return services;
        }
    }
}
