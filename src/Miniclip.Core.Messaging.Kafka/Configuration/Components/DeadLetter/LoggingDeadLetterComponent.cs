using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miniclip.Core.Messaging.Configuration;
using Miniclip.Core.Messaging.Kafka.DeadLetter;

namespace Miniclip.Core.Messaging.Kafka.Configuration.Components.DeadLetter;

internal sealed class LoggingDeadLetterComponent : IMessagingComponent
{
    public void Register(IServiceCollection services) =>
        services.TryAddSingleton<IDeadLetterHandler, LoggingDeadLetterHandler>();
}
