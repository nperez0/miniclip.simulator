using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Miniclip.Simulator.ReadModels.WebJob.Infrastructure;

public sealed partial class HealthCheckHttpServerService(
    HealthCheckService healthCheckService,
    HealthCheckConfig options,
    ILogger<HealthCheckHttpServerService> logger) : BackgroundService
{
    private readonly HttpListener listener = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var port = options.Port;
        listener.Prefixes.Add($"http://localhost:{port}/");

        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            LogFailedToStartHealthCheckHttpListenerOnPort(logger, port!);
            throw;
        }

        LogHealthCheckHttpListenerStartedOnPort(logger, port!);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var context = await listener.GetContextAsync().WaitAsync(stoppingToken);
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
            listener.Stop();
            listener.Close();
            LogHealthCheckHttpListenerStopped(logger);
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
            LogHealthCheckHttpListenerReceivedRequestForUnknownPath(logger, path);
            context.Response.StatusCode = 404;
        }

        context.Response.Close();
    }

    public override void Dispose()
    {
        listener.Close();
        base.Dispose();
    }

    [LoggerMessage(LogLevel.Information, "Health check HTTP listener started on port {Port}")]
    static partial void LogHealthCheckHttpListenerStartedOnPort(ILogger<HealthCheckHttpServerService> logger, string Port);

    [LoggerMessage(LogLevel.Critical, "Failed to start health check HTTP listener on port {Port}")]
    static partial void LogFailedToStartHealthCheckHttpListenerOnPort(ILogger<HealthCheckHttpServerService> logger, string Port);

    [LoggerMessage(LogLevel.Warning, "Health check HTTP listener received request for unknown path {Path}")]
    static partial void LogHealthCheckHttpListenerReceivedRequestForUnknownPath(ILogger<HealthCheckHttpServerService> logger, string Path);

    [LoggerMessage(LogLevel.Information, "Health check HTTP listener stopped")]
    static partial void LogHealthCheckHttpListenerStopped(ILogger<HealthCheckHttpServerService> logger);
}
