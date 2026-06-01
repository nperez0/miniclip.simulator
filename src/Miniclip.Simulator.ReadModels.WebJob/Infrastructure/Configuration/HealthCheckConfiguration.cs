using Microsoft.Extensions.Diagnostics.HealthChecks;
using Miniclip.Core.ServiceDefaults.Configuration;
using Miniclip.Core.ServiceDefaults.HealthChecks;
using Miniclip.Simulator.Infrastructure.Read.Persistence;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure.Configuration;

public static class HealthCheckConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHealthChecksDependencies(IConfiguration configuration)
        {
            services.AddHealthCheckHttpServer(configuration);

            services.AddHealthChecks()
                .AddDbContextCheck<SimulatorReadDbContext>("mysql", tags: ["ready"]);

            return services;
        }
    }
}
