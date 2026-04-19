namespace Miniclip.Core.Messaging.Pipeline.Inbound;

public interface IMessageHandlerRegistry
{
    CompiledMessageHandler? TryGet(string messageTypeName);
}
