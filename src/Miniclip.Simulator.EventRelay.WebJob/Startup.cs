using Miniclip.Simulator.EventRelay.WebJob.Infrastructure.Configuration;

namespace Miniclip.Simulator.EventRelay.WebJob;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEventRelayDependencies(configuration);
        services.AddKafkaDependencies(configuration);
        services.AddOpenTelemetryDependencies();
        services.AddHealthChecksDependencies(configuration);
    }
}
