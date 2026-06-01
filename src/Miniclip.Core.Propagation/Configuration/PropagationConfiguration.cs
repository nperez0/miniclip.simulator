using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Miniclip.Core.Propagation.Configuration;

public static class PropagationConfiguration
{
    public static IServiceCollection AddPropagationContext(this IServiceCollection services)
    {
        services.TryAddScoped<PropagationContext>();
        services.TryAddScoped<IPropagationContext>(sp => sp.GetRequiredService<PropagationContext>());
        services.TryAddScoped<IMutablePropagationContext>(sp => sp.GetRequiredService<PropagationContext>());

        return services;
    }
}
