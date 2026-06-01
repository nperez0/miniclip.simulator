using Grpc.Core;
using KurrentDB.Client;
using Miniclip.Core.Application.IntegrationEvents;
using Miniclip.Core.EventSourcing;
using Miniclip.Core.EventSourcing.EventStoreDB;
using Miniclip.Core.Extensions;
using Miniclip.Core.Messaging;
using Miniclip.Core.OpenTelemetry;
using Miniclip.Core.Propagation;
using System.Diagnostics;
using System.Text.Json;

namespace Miniclip.Simulator.EventRelay.WebJob;

public sealed partial class KurrentDbForwarderService(
    KurrentDBPersistentSubscriptionsClient subscriptionsClient,
    IDomainEventSerializer serializer,
    IIntegrationEventMapperRegistry mapperRegistry,
    IEventBus eventBus,
    IMutablePropagationContext propagationContext,
    ILogger<KurrentDbForwarderService> logger) : BackgroundService
{
    private const string SubscriptionGroupName = "simulator-kurrentdb-to-kafka-forwarder";
    private const int MaxRetryCount = 5;
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ForwardAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogForwarderFaulted(logger, ex, (int)ReconnectDelay.TotalSeconds);
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
        }
    }

    private async Task ForwardAsync(CancellationToken cancellationToken)
    {
        await using var subscription = subscriptionsClient.SubscribeToAll(
            SubscriptionGroupName,
            cancellationToken: cancellationToken);

        LogSubscribed(logger, SubscriptionGroupName);

        await foreach (var message in subscription.Messages.WithCancellation(cancellationToken))
        {
            if (message is not PersistentSubscriptionMessage.Event(var resolvedEvent, var currentRetryCount))
                continue;

            var retryCount = currentRetryCount ?? 0;

            await HandleEventAsync(subscription, resolvedEvent, retryCount, cancellationToken);
        }
    }

    private async Task HandleEventAsync(
        KurrentDBPersistentSubscriptionsClient.PersistentSubscriptionResult subscription,
        ResolvedEvent resolvedEvent,
        int retryCount,
        CancellationToken cancellationToken)
    {
        var eventType = resolvedEvent.Event.EventType;

        try
        {
            var domainEvent = serializer.Deserialize(eventType, resolvedEvent.Event.Data.ToArray());
            var integrationEvent = mapperRegistry.TryMap(domainEvent);

            if (integrationEvent is null)
            {
                await subscription.Ack(resolvedEvent);
                return;
            }

            var aggregateId = ParseAggregateId(resolvedEvent.Event.EventStreamId);
            var aggregateType = ParseAggregateType(resolvedEvent.Event.EventStreamId);

            var parentContext = RestorePropagationContext(resolvedEvent.Event.Metadata.ToArray());
            using var activity = OpenTelemetryActivity.StartActivity(
                $"{OpenTelemetryConstants.ActivitySources.SimulatorSourceName} forward {eventType}",
                ActivityKind.Producer,
                parentContext: parentContext);

            var headers = new Dictionary<string, string?>
            {
                [MessageHeaders.EventId] = resolvedEvent.Event.EventId.ToGuid().ToString(),
                [MessageHeaders.EventType] = eventType,
                [MessageHeaders.OccurredOn] = resolvedEvent.Event.Created.ToRoundTripString(),
                [MessageHeaders.AggregateId] = aggregateId,
                [MessageHeaders.AggregateType] = aggregateType,
                [MessageHeaders.AggregateVersion] = ((long)(ulong)resolvedEvent.Event.EventNumber).ToString(),
            };

            await eventBus.PublishAsync(integrationEvent, aggregateId, headers, cancellationToken);

            await subscription.Ack(resolvedEvent);

            LogEventForwarded(logger, eventType, resolvedEvent.Event.EventId.ToGuid());
        }
        catch (InvalidOperationException)
        {
            await subscription.Ack(resolvedEvent);
        }
        catch (Exception ex)
        {
            if (retryCount >= MaxRetryCount)
            {
                LogEventParked(logger, eventType, resolvedEvent.Event.EventId.ToGuid(), retryCount);
                await subscription.Nack(PersistentSubscriptionNakEventAction.Park, ex.Message, resolvedEvent);
            }
            else
            {
                LogEventRetried(logger, eventType, resolvedEvent.Event.EventId.ToGuid(), retryCount);
                await subscription.Nack(PersistentSubscriptionNakEventAction.Retry, ex.Message, resolvedEvent);
            }
        }
    }

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        var eventTypeNames = mapperRegistry.MappedDomainEventTypeNames;

        if (eventTypeNames.Count == 0)
        {
            LogNoMappersRegistered(logger);
            return;
        }

        var filter = KurrentDB.Client.EventTypeFilter.RegularExpression(
            $"^({string.Join("|", eventTypeNames.Select(System.Text.RegularExpressions.Regex.Escape))})$");

        var settings = new PersistentSubscriptionSettings(startFrom: Position.Start);

        try
        {
            await subscriptionsClient.CreateToAllAsync(
                SubscriptionGroupName,
                filter,
                settings,
                cancellationToken: cancellationToken);

            LogSubscriptionCreated(logger, SubscriptionGroupName);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.AlreadyExists)
        {
            LogSubscriptionAlreadyExists(logger, SubscriptionGroupName);
        }
    }

    private ActivityContext? RestorePropagationContext(byte[] metadata)
    {
        if (metadata.Length == 0)
            return null;

        var eventMetadata = JsonSerializer.Deserialize<EventMetadata>(metadata);
        if (eventMetadata is null)
            return null;

        propagationContext.CorrelationId = eventMetadata.CorrelationId;
        propagationContext.CausationId = eventMetadata.CausationId;

        if (eventMetadata.TraceParent.IsNullOrEmpty())
            return null;

        return ActivityContext.TryParse(eventMetadata.TraceParent, eventMetadata.TraceState, isRemote: true, out var ctx)
            ? ctx
            : null;
    }

    private static string ParseAggregateId(string streamId)
    {
        var separatorIndex = streamId.LastIndexOf('-');
        return separatorIndex >= 0 ? streamId[(separatorIndex + 1)..] : streamId;
    }

    private static string ParseAggregateType(string streamId)
    {
        var separatorIndex = streamId.LastIndexOf('-');
        return separatorIndex >= 0 ? streamId[..separatorIndex] : streamId;
    }

    [LoggerMessage(LogLevel.Information, "KurrentDB forwarder subscribed to group '{GroupName}'")]
    static partial void LogSubscribed(ILogger logger, string GroupName);

    [LoggerMessage(LogLevel.Information, "Persistent subscription group '{GroupName}' created")]
    static partial void LogSubscriptionCreated(ILogger logger, string GroupName);

    [LoggerMessage(LogLevel.Information, "Persistent subscription group '{GroupName}' already exists — skipping creation")]
    static partial void LogSubscriptionAlreadyExists(ILogger logger, string GroupName);

    [LoggerMessage(LogLevel.Information, "Event '{EventType}' ({EventId}) forwarded to Kafka")]
    static partial void LogEventForwarded(ILogger logger, string EventType, Guid EventId);

    [LoggerMessage(LogLevel.Warning, "Event '{EventType}' ({EventId}) parked after {RetryCount} retries")]
    static partial void LogEventParked(ILogger logger, string EventType, Guid EventId, int RetryCount);

    [LoggerMessage(LogLevel.Warning, "Event '{EventType}' ({EventId}) nacked for retry (attempt {RetryCount})")]
    static partial void LogEventRetried(ILogger logger, string EventType, Guid EventId, int RetryCount);

    [LoggerMessage(LogLevel.Warning, "No integration event mappers registered — forwarder will not start subscription")]
    static partial void LogNoMappersRegistered(ILogger logger);

    [LoggerMessage(LogLevel.Error, "KurrentDB forwarder faulted — reconnecting in {DelaySeconds}s")]
    static partial void LogForwarderFaulted(ILogger logger, Exception ex, int DelaySeconds);
}
