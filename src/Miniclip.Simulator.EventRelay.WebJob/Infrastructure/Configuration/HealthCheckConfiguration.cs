using Miniclip.Core.ServiceDefaults.Configuration;

namespace Miniclip.Simulator.EventRelay.WebJob.Infrastructure.Configuration;

public static class HealthCheckConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHealthChecksDependencies(IConfiguration configuration)
        {
            services.AddHealthCheckHttpServer(configuration);

            return services;
        }
    }
}
