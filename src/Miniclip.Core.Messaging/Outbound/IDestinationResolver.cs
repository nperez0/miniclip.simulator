namespace Miniclip.Core.Messaging.Outbound;

public interface IDestinationResolver
{
    string Resolve(OutboundEnvelope envelope);
}
