using Miniclip.Core.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Miniclip.Core.ServiceDefaults;

public static class SerilogConfiguration
{
    public static IHostApplicationBuilder AddStructuredLogging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, config) =>
        {
            var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

            config
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());

            if (otlpEndpoint.IsNotNullOrWhiteSpace())
            {
                config.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint;
                    options.Protocol = OtlpProtocol.Grpc;
                });
            }
        });

        return builder;
    }
}
