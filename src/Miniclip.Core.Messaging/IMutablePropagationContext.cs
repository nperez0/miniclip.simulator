namespace Miniclip.Core.Messaging;

/// <summary>
/// Mutable view of the ambient propagation context, used by middleware and HTTP middleware
/// to set the correlation/causation IDs for the current scope.
/// </summary>
public interface IMutablePropagationContext : IPropagationContext
{
    new Guid CorrelationId { get; set; }
    new Guid CausationId { get; set; }
    void SetHeader(string key, string value);
}
