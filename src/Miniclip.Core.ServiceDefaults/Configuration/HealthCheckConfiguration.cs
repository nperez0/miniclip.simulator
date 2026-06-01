using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Miniclip.Core.Extensions;
using Miniclip.Core.ServiceDefaults.HealthChecks;

namespace Miniclip.Core.ServiceDefaults.Configuration;

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddHealthCheckHttpServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        var config = new HealthCheckConfig
        {
            Port = configuration[HealthCheckConfig.HealthCheckHttpPortListenerKey]
        };

        if (config.Port.IsNullOrEmpty())
            return services;

        services.AddSingleton(config);
        services.AddHostedService<HealthCheckHttpServerService>();

        return services;
    }
}
