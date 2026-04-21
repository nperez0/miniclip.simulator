namespace Miniclip.Core.Messaging;

public interface IPropagationContext
{
    Guid CorrelationId { get; }
    Guid CausationId { get; }
    IReadOnlyDictionary<string, string> AdditionalHeaders { get; }
}
