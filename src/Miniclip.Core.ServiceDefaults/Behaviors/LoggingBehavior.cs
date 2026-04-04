using Mediator;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Miniclip.Core.ServiceDefaults.Behaviors;

public partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ActivitySource ActivitySource = new("Miniclip.Mediator");

    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var activity = ActivitySource.StartActivity(requestName);
        activity?.SetTag("request.type", requestName);

        LogHandlingRequest(logger, requestName);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next(request, cancellationToken);

            sw.Stop();

            if (response is Result { IsFailure: true } failed)
            {
                activity?.SetStatus(ActivityStatusCode.Error, failed.Error.Code);
                activity?.SetTag("error.code", failed.Error.Code);
                activity?.SetTag("error.type", failed.Error.Type.ToString());

                if (failed.Error.Type == ErrorType.Conflict)
                    LogConflict(logger, requestName, failed.Error.Code, sw.ElapsedMilliseconds);
                else
                    LogDomainFailure(logger, requestName, failed.Error.Code,
                        failed.Error.Type.ToString(), sw.ElapsedMilliseconds);
            }
            else
                LogHandledRequest(logger, requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogUnhandledException(logger, ex, requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }

    [LoggerMessage(LogLevel.Information, "Handling {RequestName}")]
    static partial void LogHandlingRequest(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger, 
        string RequestName);

    [LoggerMessage(LogLevel.Information, "Handled {RequestName} in {ElapsedMs}ms")]
    static partial void LogHandledRequest(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger, 
        string RequestName, 
        long ElapsedMs);

    [LoggerMessage(LogLevel.Warning,
        "Domain failure in {RequestName}: [{ErrorCode}] ({ErrorType}) in {ElapsedMs}ms")]
    static partial void LogDomainFailure(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        string RequestName, 
        string ErrorCode, 
        string ErrorType, 
        long ElapsedMs);

    [LoggerMessage(LogLevel.Error,
        "Conflict in {RequestName}: [{ErrorCode}] in {ElapsedMs}ms")]
    static partial void LogConflict(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        string RequestName, 
        string ErrorCode, 
        long ElapsedMs);

    [LoggerMessage(LogLevel.Error, "Unhandled exception in {RequestName} after {ElapsedMs}ms")]
    static partial void LogUnhandledException(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        Exception ex, 
        string RequestName, 
        long ElapsedMs);
}
