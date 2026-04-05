using Miniclip.Core.OpenTelemetry.Constants;
using System.Diagnostics;

namespace Miniclip.Core.OpenTelemetry.Activities;

public class OpenTelemetryActivityService
{
    private static readonly ActivitySource ActivitySourceInstance = new ActivitySource(OpenTelemetryConstants.ActivitySources.OpenTelemetryActivitySourceName);

    public void SetTag(string key, object value)
    {
        Activity.Current?.AddTag(key, value);
    }

    public void NoticeError(Exception exception, IEnumerable<KeyValuePair<string, object?>>? attributes = null)
    {
        Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.Message);
        Activity.Current?.AddException(exception);

        foreach (var attr in attributes ?? [])
            Activity.Current?.AddTag(attr.Key, attr.Value);
    }

    public OpenTelemetryActivity StartActivity(string name, IEnumerable<KeyValuePair<string, object?>>? initialTags)
    {
        var activity = ActivitySourceInstance.StartActivity(name, ActivityKind.Server, default(ActivityContext), initialTags);

        return new OpenTelemetryActivity(activity);
    }
}
