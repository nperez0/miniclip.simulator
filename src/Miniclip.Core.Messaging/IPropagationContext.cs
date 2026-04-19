namespace Miniclip.Core.Messaging;

/// <summary>
/// Read-only view of the ambient propagation context (correlation/causation IDs and extra headers).
/// </summary>
public interface IPropagationContext
{
    Guid CorrelationId { get; }
    Guid CausationId { get; }
    IReadOnlyDictionary<string, string> AdditionalHeaders { get; }
}
