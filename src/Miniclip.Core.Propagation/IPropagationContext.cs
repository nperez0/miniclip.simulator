namespace Miniclip.Core.Propagation;

public interface IPropagationContext
{
    Guid CorrelationId { get; }
    Guid CausationId { get; }
    IReadOnlyDictionary<string, string> AdditionalHeaders { get; }
}
