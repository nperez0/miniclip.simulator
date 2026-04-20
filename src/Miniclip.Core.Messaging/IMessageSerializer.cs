namespace Miniclip.Core.Messaging;

public interface IMessageSerializer
{
    byte[] Serialize(object @event);
    object Deserialize(string messageType, ReadOnlyMemory<byte> payload);
}
