using Microsoft.Extensions.Diagnostics.HealthChecks;
using Miniclip.Simulator.ReadModels.WebJob.Infrastructure;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

public static class HealthCheckConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHealthChecksDependencies(IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

            var config = new HealthCheckConfig
            {
                Port = configuration[HealthCheckConfig.HealthCheckHttpPortListenerKey]
            };

            if (string.IsNullOrEmpty(config.Port)) 
                return services;

            services.AddSingleton(config);
            services.AddHostedService<HealthCheckHttpServerService>();

            return services;
        }
    }
}
