using Microsoft.Extensions.Diagnostics.HealthChecks;
using Miniclip.Simulator.Api.Infrastructure.Configuration;
using Serilog;

namespace Miniclip.Simulator.Api;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddApiVersioningConfiguration();
        services.AddVersionedOpenApi();

        services.AddServiceDiscovery();

        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        services.AddKafkaDependencies(configuration);
        services.AddMediatorDependencies();
        services.AddEventStoreDbDependencies(configuration);
        services.AddReadModelsDbDependencies(configuration);
        services.AddDomainDependencies();

        services.AddOpenTelemetryDependencies();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging();

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapVersionedOpenApi();
        });
    }
}
