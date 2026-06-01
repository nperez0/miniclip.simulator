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
                        .AddMeter(Mediator.Mediator.MeterName)
                        .AddSimulator();
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter()
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
