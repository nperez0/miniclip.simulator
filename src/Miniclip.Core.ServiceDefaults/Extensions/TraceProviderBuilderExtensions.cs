using OpenTelemetry.Trace;
using static Miniclip.Core.OpenTelemetry.Constants.OpenTelemetryConstants;

namespace Miniclip.Core.ServiceDefaults.Extensions;

public static class TraceProviderBuilderExtensions
{
    extension(TracerProviderBuilder builder)
    {
        public TracerProviderBuilder AddMySqlData()
        {
            builder.AddSource(ActivitySources.MySqlData);
            return builder;
        }

        public TracerProviderBuilder AddMySqlConnector()
        {
            builder.AddSource(ActivitySources.MySqlConnector);
            return builder;
        }
    }
}
