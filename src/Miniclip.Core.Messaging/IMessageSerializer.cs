namespace Miniclip.Core.Messaging;

public interface IMessageSerializer
{
    string Serialize(object @event);
}
