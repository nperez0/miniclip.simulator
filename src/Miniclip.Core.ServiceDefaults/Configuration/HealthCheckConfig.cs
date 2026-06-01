namespace Miniclip.Core.ServiceDefaults.Configuration;

public sealed record HealthCheckConfig
{
    public const string HealthCheckHttpPortListenerKey = "HEALTHCHECK_HTTP_PORT_LISTENER";

    public string? Port { get; init; }
}
