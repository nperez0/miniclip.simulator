using Microsoft.Extensions.Diagnostics.HealthChecks;
using Miniclip.Core.Propagation.Configuration;
using Miniclip.Simulator.Api.Infrastructure.Middleware;
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

        services.AddMediatorDependencies();
        services.AddEventStoreDependencies(configuration);
        services.AddReadModelsDbDependencies(configuration);
        services.AddDomainDependencies();
        services.AddPropagationContext();

        services.AddOpenTelemetryDependencies();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();

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
