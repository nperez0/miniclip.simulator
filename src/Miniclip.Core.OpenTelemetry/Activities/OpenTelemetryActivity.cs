using System.Diagnostics;

namespace Miniclip.Core.OpenTelemetry.Activities;

public class OpenTelemetryActivity(Activity? activity) : IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (disposing)
            activity?.Stop();
    }
}
