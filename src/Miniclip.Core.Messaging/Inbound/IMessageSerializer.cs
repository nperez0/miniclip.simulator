namespace Miniclip.Core.Messaging.Inbound;

public interface IMessageSerializer
{
    byte[] Serialize(object @event);
    object Deserialize(string messageType, ReadOnlyMemory<byte> payload);
}
