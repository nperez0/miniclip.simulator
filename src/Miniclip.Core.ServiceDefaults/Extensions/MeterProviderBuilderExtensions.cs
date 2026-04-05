using OpenTelemetry.Metrics;
using static Miniclip.Core.OpenTelemetry.OpenTelemetryConstants;

namespace Miniclip.Core.ServiceDefaults.Extensions;

public static class MeterProviderBuilderExtensions
{
    extension(MeterProviderBuilder builder)
    {
        public MeterProviderBuilder AddSimulator()
        {
            builder.AddMeter(Metrics.SimulatorMetricName);
            return builder;
        }
    }
}
