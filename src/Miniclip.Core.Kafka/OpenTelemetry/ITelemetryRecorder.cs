namespace Miniclip.Core.Kafka.OpenTelemetry;

public interface ITelemetryRecorder : IDisposable
{
    void RecordRetryAttempt();
    void RecordMessageFailed();
    void RecordProcessingDuration();
    void SetErrorStatus(Exception exception);
}
