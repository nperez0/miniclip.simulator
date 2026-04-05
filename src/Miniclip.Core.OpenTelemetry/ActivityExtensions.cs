using System.Diagnostics;

namespace Miniclip.Core.OpenTelemetry;

public static class ActivityExtensions
{
    public static void NoticeError(this Activity? activity, string message, IEnumerable<KeyValuePair<string, object?>>? attributes = null)
    {
        activity?.SetStatus(ActivityStatusCode.Error, message);

        foreach (var attr in attributes ?? [])
            activity?.AddTag(attr.Key, attr.Value);
    }
}
