using System.Diagnostics;

namespace Miniclip.Core.OpenTelemetry;

public class OpenTelemetryActivity(Activity? activity) : IDisposable
{
    private static readonly ActivitySource ActivitySourceInstance = new ActivitySource(OpenTelemetryConstants.ActivitySources.SimulatorSourceName);

    public void SetTag(string key, object value)
    {
        activity?.AddTag(key, value);
    }

    public void NoticeError(Exception exception, IEnumerable<KeyValuePair<string, object?>>? attributes = null)
    {
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.AddException(exception);

        foreach (var attr in attributes ?? [])
            activity?.AddTag(attr.Key, attr.Value);
    }

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

    public static OpenTelemetryActivity StartActivity(
        string name,
        ActivityKind kind = ActivityKind.Server,
        IEnumerable<KeyValuePair<string, object?>>? initialTags = null,
        ActivityContext? parentContext = null)
    {
        var parent = parentContext ?? default(ActivityContext);
        var activity = ActivitySourceInstance.StartActivity(name, kind, parent, initialTags);

        return new OpenTelemetryActivity(activity);
    }
}
