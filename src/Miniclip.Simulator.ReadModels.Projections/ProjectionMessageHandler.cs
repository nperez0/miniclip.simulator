using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Domain;
using Miniclip.Core.Messaging;
using Miniclip.Core.ReadModels;
using Miniclip.Core.ReadModels.Projections;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections;

public sealed partial class ProjectionMessageHandler<TEvent>(
    IServiceScopeFactory scopeFactory,
    ILogger<ProjectionMessageHandler<TEvent>> logger)
    : IMessageHandler<TEvent>
    where TEvent : IDomainEvent
{
    public async Task<MessageHandlerResult> HandleAsync(
        TEvent message,
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var processedEventsRepository = scope.ServiceProvider.GetRequiredService<IProcessedEventsRepository>();

        // Idempotency check
        if (await processedEventsRepository.ContainsAsync(
            context.MessageId, context.SubscriptionId, cancellationToken))
        {
            LogEventSkipped(logger, context.MessageId, typeof(TEvent).Name, context.SubscriptionId);
            return MessageHandlerResult.Success();
        }

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IReadModelUnitOfWork>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IProjectionDispatcher>();

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Dispatch to registered projections
            await dispatcher.DispatchAsync(message, cancellationToken);

            // Record that we've processed this event (for idempotency)
            processedEventsRepository.Add(context.MessageId, context.SubscriptionId);

            // Persist projection updates
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            LogEventProjected(logger, context.MessageId, typeof(TEvent).Name, context.SubscriptionId);
            return MessageHandlerResult.Success();
        }
        catch (DbUpdateException ex) when (IsTransient(ex))
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            LogTransientError(logger, ex, context.MessageId);
            return MessageHandlerResult.TransientFailure(ex.Message);
        }
        catch (OperationCanceledException)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            LogPermanentError(logger, ex, context.MessageId);
            return MessageHandlerResult.PermanentFailure(ex.Message);
        }
    }

    private static bool IsTransient(DbUpdateException ex)
    {
        // Check if the inner exception indicates a transient error
        if (ex.InnerException is not null)
        {
            var exceptionTypeName = ex.InnerException.GetType().FullName;

            // MySQL transient errors
            if (exceptionTypeName?.Contains("MySql") ?? false)
            {
                var isTransient = ex.InnerException.GetType()
                    .GetProperty("IsTransient")
                    ?.GetValue(ex.InnerException) as bool? ?? false;
                return isTransient;
            }

            // SqlServer transient errors
            if (exceptionTypeName?.Contains("SqlClient") ?? false)
            {
                var number = ex.InnerException.GetType()
                    .GetProperty("Number")
                    ?.GetValue(ex.InnerException) as int? ?? -1;

                // SQL Server transient error numbers: -1 (timeout), -2 (network), 64, 233, 20, 64, 40197, 40501, 40613, 40540, 40544, 40549, 40550, 40551, 40552, 40553
                return number is -1 or -2 or 64 or 233 or 20 or 40197 or 40501 or 40613 or 40540 or 40544 or 40549 or 40550 or 40551 or 40552 or 40553;
            }
        }

        return false;
    }

    [LoggerMessage(LogLevel.Information,
        "Event {EventId} ({EventType}) skipped — already processed by {SubscriptionId}")]
    static partial void LogEventSkipped(
        ILogger logger, string EventId, string EventType, string SubscriptionId);

    [LoggerMessage(LogLevel.Information,
        "Event {EventId} ({EventType}) projected successfully by {SubscriptionId}")]
    static partial void LogEventProjected(
        ILogger logger, string EventId, string EventType, string SubscriptionId);

    [LoggerMessage(LogLevel.Warning,
        "Event {EventId}: Transient error, will retry")]
    static partial void LogTransientError(
        ILogger logger, Exception ex, string EventId);

    [LoggerMessage(LogLevel.Error,
        "Event {EventId}: Permanent error, sending to DLQ")]
    static partial void LogPermanentError(
        ILogger logger, Exception ex, string EventId);
}
