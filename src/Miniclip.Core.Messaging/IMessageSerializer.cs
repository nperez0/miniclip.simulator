namespace Miniclip.Core.Messaging;

public interface IMessageSerializer
{
    object Deserialize(string messageType, ReadOnlyMemory<byte> payload);
}
