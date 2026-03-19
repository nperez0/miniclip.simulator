using Mediator;
using Miniclip.Core.ReadModels.Projections.Attributes;
using System.Reflection;

namespace Miniclip.Core.Application.Behaviors;

public class OrderedNotificationPublisher : INotificationPublisher
{
    public async ValueTask Publish<TNotification>(
        NotificationHandlers<TNotification> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var orderedHandlers = ((IEnumerable<INotificationHandler<TNotification>>)handlers)
            .OrderBy(handler => handler.GetType()
                .GetCustomAttribute<HandlerPriorityAttribute>()?.Priority ?? int.MaxValue);

        foreach (var handler in orderedHandlers)
            await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
    }
}
