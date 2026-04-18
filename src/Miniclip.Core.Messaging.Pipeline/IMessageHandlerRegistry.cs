namespace Miniclip.Core.Messaging.Pipeline;

public interface IMessageHandlerRegistry
{
    CompiledMessageHandler? TryGet(string messageTypeName);
}
