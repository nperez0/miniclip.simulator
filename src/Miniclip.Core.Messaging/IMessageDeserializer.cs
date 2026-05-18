namespace Miniclip.Core.Messaging;

public interface IMessageDeserializer
{
    object Deserialize(string messageType, string payload);
}
