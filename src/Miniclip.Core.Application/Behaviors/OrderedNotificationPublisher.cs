using MediatR;
using Miniclip.Core.ReadModels.Projections.Attributes;
using System.Reflection;

namespace Miniclip.Core.Application.Behaviors;

public class OrderedNotificationPublisher : INotificationPublisher
{
    public async Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken)
    {
        var orderedExecutors = handlerExecutors
            .OrderBy(executor => executor.HandlerInstance.GetType()
                .GetCustomAttribute<HandlerPriorityAttribute>()?.Priority ?? int.MaxValue);

        foreach (var executor in orderedExecutors)
            await executor.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);

    }
}
