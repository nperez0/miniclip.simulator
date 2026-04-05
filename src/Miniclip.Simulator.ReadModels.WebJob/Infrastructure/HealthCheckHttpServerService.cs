using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure;

public sealed class HealthCheckHttpServerService(
    HealthCheckService healthCheckService,
    IConfiguration configuration,
    ILogger<HealthCheckHttpServerService> logger) : BackgroundService
{
    public const string HealthCheckHttpPortListenerKey = "HEALTHCHECK_HTTP_PORT_LISTENER";

    private readonly HttpListener _listener = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var port = configuration[HealthCheckHttpPortListenerKey];
        _listener.Prefixes.Add($"http://localhost:{port}/");

        try
        {
            _listener.Start();
        }
        catch (HttpListenerException ex)
        {
            logger.LogCritical(ex, "Failed to start health check HTTP listener on port {Port}", port);
            throw;
        }

        logger.LogInformation("Health check HTTP listener started on port {Port}", port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync().WaitAsync(stoppingToken);
                    _ = HandleRequestAsync(context, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (HttpListenerException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            _listener.Stop();
            _listener.Close();
            logger.LogInformation("Health check HTTP listener stopped");
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken stoppingToken)
    {
        var path = context.Request.Url?.AbsolutePath ?? string.Empty;

        if (path is "/health" or "/alive")
        {
            var report = await healthCheckService.CheckHealthAsync(stoppingToken);
            var isHealthy = report.Status == HealthStatus.Healthy;

            context.Response.StatusCode = isHealthy ? 200 : 503;
            context.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(new { status = report.Status.ToString() });
            var buffer = Encoding.UTF8.GetBytes(body);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, stoppingToken);
        }
        else
        {
            logger.LogWarning("Health check HTTP listener received request for unknown path {Path}", path);
            context.Response.StatusCode = 404;
        }

        context.Response.Close();
    }

    public override void Dispose()
    {
        _listener.Close();
        base.Dispose();
    }
}
