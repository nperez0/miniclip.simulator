using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Miniclip.Core.Kafka.OpenTelemetry;

internal sealed partial class KafkaMessageContextTelemetry(TagList tags, Activity? activity, ILogger logger)
    : ITelemetryRecorder
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public void RecordRetryAttempt()
    {
        try { Telemetry.RetryAttempts.Add(1, tags); }
        catch (Exception ex) { LogTelemetryError(logger, ex); }
    }

    public void RecordMessageFailed()
    {
        try { Telemetry.MessagesFailed.Add(1, tags); }
        catch (Exception ex) { LogTelemetryError(logger, ex); }
    }

    public void RecordProcessingDuration()
    {
        try { Telemetry.ProcessingDuration.Record(_stopwatch.Elapsed.TotalMilliseconds, tags); }
        catch (Exception ex) { LogTelemetryError(logger, ex); }
    }

    public void SetErrorStatus(Exception exception)
    {
        try { activity?.SetStatus(ActivityStatusCode.Error, exception.Message); }
        catch (Exception ex) { LogTelemetryError(logger, ex); }
    }

    public void Dispose()
    {
        try { activity?.Dispose(); }
        catch (Exception ex) { LogTelemetryError(logger, ex); }
    }

    [LoggerMessage(LogLevel.Warning, "Telemetry recording failed")]
    static partial void LogTelemetryError(ILogger logger, Exception ex);
}
