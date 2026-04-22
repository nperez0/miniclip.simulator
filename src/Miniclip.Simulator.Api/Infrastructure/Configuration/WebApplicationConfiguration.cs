using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Miniclip.Simulator.Api.Infrastructure.Configuration;

public static class WebApplicationConfiguration
{
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");

        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
