namespace Miniclip.Core.Messaging;

public sealed class PropagationContext : IMutablePropagationContext
{
    private readonly Dictionary<string, string> additionalHeaders = [];

    public Guid CorrelationId { get; set; } = Guid.NewGuid();

    public Guid CausationId { get; set; }

    public IReadOnlyDictionary<string, string> AdditionalHeaders => additionalHeaders;

    public PropagationContext()
    {
        // By default CausationId equals CorrelationId (no upstream cause).
        CausationId = CorrelationId;
    }

    public void SetHeader(string key, string value) => additionalHeaders[key] = value;
}
