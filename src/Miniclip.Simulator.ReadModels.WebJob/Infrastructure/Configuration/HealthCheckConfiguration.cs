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

            var port = configuration[HealthCheckHttpServerService.HealthCheckHttpPortListenerKey];
            if (!string.IsNullOrEmpty(port))
            {
                services.AddHostedService<HealthCheckHttpServerService>();
            }

            return services;
        }
    }
}
