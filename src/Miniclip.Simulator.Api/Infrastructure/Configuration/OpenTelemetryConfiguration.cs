using KurrentDB.Client.Extensions.OpenTelemetry;
using Miniclip.Core.OpenTelemetry.Extensions;
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
                        .AddMeter(Mediator.Mediator.MeterName)
                        .AddSimulator();
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter()
                        .AddKafkaProducerInstrumentation<string, byte[]>()
                        .AddSource(Mediator.Mediator.ActivitySourceName)
                        .AddMySqlData()
                        .AddMySqlConnector()
                        .AddKurrentDBClientInstrumentation()
                        .AddSimulator();
                });

            return services;
        }
    }
}
