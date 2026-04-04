using Confluent.Kafka;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Miniclip.Core.Kafka.OpenTelemetry;

internal static class PropagationExtensions
{
    internal static void InjectTraceContext(this Message<string, byte[]> message, string topic, ILogger logger)
    {
        try
        {
            // Inject current trace context so the consumer can link its span to this one
            var propagationContext = new PropagationContext(
                Activity.Current?.Context ?? default,
                Baggage.Current);

            Propagators.DefaultTextMapPropagator.Inject(
                propagationContext,
                message.Headers,
                static (headers, key, value) =>
                    headers.Add(key, Encoding.UTF8.GetBytes(value)));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to inject trace context into message for topic {Topic}", topic);
        }
    }
}
