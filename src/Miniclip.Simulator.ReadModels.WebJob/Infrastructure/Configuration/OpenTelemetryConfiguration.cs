using Miniclip.Core.Messaging.Kafka;
using Miniclip.Core.Messaging.Pipeline.Inbound;
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
                metrics.AddKafkaProducerInstrumentation<string, string>();

                foreach (var subscription in sp.GetServices<ConsumerSubscription>())
                    metrics.AddKafkaConsumerInstrumentation<string, string>(subscription.SubscriptionId);
            });

            services.ConfigureOpenTelemetryTracerProvider((sp, tracing) =>
            {
                tracing.AddKafkaProducerInstrumentation<string, string>();

                foreach (var subscription in sp.GetServices<ConsumerSubscription>())
                    tracing.AddKafkaConsumerInstrumentation<string, string>(subscription.SubscriptionId);
            });

            return services;
        }
    }
}
