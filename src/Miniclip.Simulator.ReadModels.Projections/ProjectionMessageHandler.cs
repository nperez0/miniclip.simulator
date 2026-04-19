using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Miniclip.Core.Domain;
using Miniclip.Core.Messaging;
using Miniclip.Core.Messaging.Inbound;
using Miniclip.Core.ReadModels;
using Miniclip.Core.ReadModels.Projections;
using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.ReadModels.Projections;

public sealed partial class ProjectionMessageHandler<TEvent>(
    IProcessedEventsRepository processedEventsRepository,
    IReadModelUnitOfWork unitOfWork,
    IProjectionDispatcher dispatcher,
    ILogger<ProjectionMessageHandler<TEvent>> logger)
    : IMessageHandler<TEvent>
    where TEvent : IDomainEvent
{
    public async Task<MessageHandlerResult> HandleAsync(
        TEvent message,
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        // Idempotency check
        if (await processedEventsRepository.ContainsAsync(
            context.MessageId, context.SubscriptionId, cancellationToken))
        {
            LogEventSkipped(logger, context.MessageId, typeof(TEvent).Name, context.SubscriptionId);
            return MessageHandlerResult.Success();
        }

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
        catch (DbUpdateException ex)
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
