using Miniclip.Core.OpenTelemetry.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Miniclip.Simulator.EventRelay.WebJob.Infrastructure.Configuration;

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
                    .AddKafkaProducerInstrumentation<string, string>()
                    .AddSimulator())
                .WithTracing(tracing => tracing
                    .AddOtlpExporter()
                    .AddKafkaProducerInstrumentation<string, string>()
                    .AddSimulator());

            return services;
        }
    }
}
