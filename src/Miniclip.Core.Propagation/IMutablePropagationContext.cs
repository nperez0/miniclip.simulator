namespace Miniclip.Core.Propagation;

public interface IMutablePropagationContext : IPropagationContext
{
    new Guid CorrelationId { get; set; }
    new Guid CausationId { get; set; }
    void SetHeader(string key, string value);
}
