namespace Miniclip.Core.Messaging.Inbound;

public interface IMessageContext
{
    string MessageId { get; }

    string SubscriptionId { get; }

    IReadOnlyDictionary<string, string> Headers { get; }
}
