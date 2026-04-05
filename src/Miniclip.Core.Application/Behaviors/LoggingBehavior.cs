using Mediator;
using Microsoft.Extensions.Logging;
using Miniclip.Core.OpenTelemetry.Extensions;
using System.Diagnostics;

namespace Miniclip.Core.Application.Behaviors;

public partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        LogHandlingRequest(logger, requestName);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next(request, cancellationToken);

            sw.Stop();

            if (response is Result { IsFailure: true } failed)
            {
                if (failed.Error.Type == ErrorType.Conflict)
                {
                    Activity.Current.NoticeError(failed.Error.Code);
                    LogConflict(logger, requestName, failed.Error.Code, sw.ElapsedMilliseconds);
                }
                else
                {
                    LogDomainFailure(
                        logger,
                        requestName,
                        failed.Error.Code,
                        failed.Error.Type.ToString(),
                        sw.ElapsedMilliseconds);
                }
            }
            else
                LogHandledRequest(logger, requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
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
