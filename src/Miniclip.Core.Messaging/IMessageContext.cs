namespace Miniclip.Core.Messaging;

public interface IMessageContext
{
    string MessageId { get; }

    string SubscriptionId { get; }

    IReadOnlyDictionary<string, string> Headers { get; }
}
